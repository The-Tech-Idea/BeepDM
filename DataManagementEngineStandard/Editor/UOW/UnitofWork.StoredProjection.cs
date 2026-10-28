using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T> : IUnitofWorkStoredProjectionRead
    {
        public virtual bool SupportsStoredProjectionRead => SupportsStagedPageRead && DataSource is IBoundedStoredProjectionDataSource &&
            GetType().GetMethod(nameof(PreparePageReadAsync),new[]{typeof(BoundedPageRequest),typeof(CancellationToken)})?.DeclaringType==typeof(UnitofWork<T>);

        public virtual async Task<StoredProjectionPage> ReadStoredProjectionPageAsync(BoundedPageRequest request,
            IEnumerable<string> fields, CancellationToken cancellationToken=default)
        {
            ArgumentNullException.ThrowIfNull(request);ArgumentNullException.ThrowIfNull(fields);
            cancellationToken.ThrowIfCancellationRequested();
            if(Interlocked.CompareExchange(ref _commitAdmission,1,0)!=0)throw new InvalidOperationException("A commit or staged read is already active.");
            Volatile.Write(ref _readAdmission,1);ReadStage stage=null;
            try
            {
                if(!SupportsStoredProjectionRead)throw new NotSupportedException("This UoW/provider does not support detached bounded stored projections.");
                if(request.ValidateReadOnlyScalars)throw new ArgumentException("A stored projection does not evaluate calculated entity getters.");
                stage=new ReadStage(this,cancellationToken,true);stage.CheckTarget();
                var selected=fields.ToArray();
                if(selected.Length is <1 or >128||selected.Any(string.IsNullOrWhiteSpace)||selected.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=selected.Length)
                    throw new ArgumentException("Select one through128 unique declared stored fields.");
                var metadata=EntityStructure.Fields.ToDictionary(field=>field.FieldName,StringComparer.OrdinalIgnoreCase);
                var properties=new Dictionary<string,PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                foreach(var name in selected)
                {
                    if(!metadata.TryGetValue(name,out var field))throw new ArgumentException("Projection fields must be declared columns, never expressions.");
                    var property=typeof(T).GetProperty(field.FieldName,BindingFlags.Instance|BindingFlags.Public|BindingFlags.IgnoreCase);
                    if(property?.CanRead!=true||property.GetIndexParameters().Length!=0)throw new NotSupportedException("A stored projection requires declared readable scalar properties.");
                    var type=Nullable.GetUnderlyingType(property.PropertyType)??property.PropertyType;
                    if(!(type.IsPrimitive||type.IsEnum||type==typeof(string)||type==typeof(decimal)||type==typeof(Guid)||
                        type==typeof(DateTime)||type==typeof(DateTimeOffset)||type==typeof(TimeSpan)||type==typeof(byte[])))
                        throw new NotSupportedException("A stored projection supports scalar and binary columns only.");
                    properties.Add(field.FieldName,property);
                }
                var keys=EntityStructure.Fields.Where(field=>field.IsKey).ToArray();
                if(keys.Length==0||keys.Any(key=>!properties.ContainsKey(key.FieldName)))throw new ArgumentException("A stored projection must include the complete primary key.");
                if(IsTenantScoped&&TenantFiltersReads&&(!properties.ContainsKey(TenantFieldName)||string.IsNullOrWhiteSpace(TenantId)))
                    throw new ArgumentException("A scoped projection must include its established tenant field.");
                var order=new List<PageOrder>();var ordered=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var column in request.Order)
                {
                    if(!properties.ContainsKey(column.FieldName)||!ordered.Add(column.FieldName))throw new ArgumentException("Projection order must use unique selected fields.");
                    order.Add(new(metadata[column.FieldName].FieldName,column.Descending));
                }
                foreach(var key in keys)if(ordered.Add(key.FieldName))order.Add(new(key.FieldName));
                var filters=request.CopyFilters();
                var args=new UnitofWorkParams{EventAction=EventAction.PreQuery};PreQuery?.Invoke(this,args);
                if(args.Cancel)throw new InvalidOperationException(args.Messege??"Projection cancelled before provider execution.");
                stage.CheckTarget();
                if(IsTenantScoped&&TenantFiltersReads)filters.Add(new(){FieldName=TenantFieldName,Operator="=",FilterValue=TenantId});
                var qualified=request.WithValues(order,filters);
                RequireStoredProjectionCapabilities(qualified);
                var response=await ((IBoundedStoredProjectionDataSource)stage.Source).ReadStoredProjectionPageAsync(stage.EntityName,qualified,
                    Array.AsReadOnly(properties.Keys.ToArray()),cancellationToken).ConfigureAwait(false);
                stage.CheckTarget();
                var result=DecodeStoredProjection(qualified,response,properties,keys.Select(key=>key.FieldName).ToArray(),cancellationToken);
                stage.CheckTarget();
                ReadOutcome=new ErrorsInfo{Flag=Errors.Ok,Message="Detached bounded stored projection acknowledged."};
                return result;
            }
            catch(Exception ex)
            {
                ReadOutcome=new ErrorsInfo{Flag=Errors.Failed,Message="Stored projection was not acknowledged.",Ex=ex};
                DMEEditor?.AddLogMessage(nameof(UnitofWork<T>),"Stored projection admission failed: "+ex,DateTime.Now,-1,null,Errors.Failed);
                throw;
            }
            finally{if(stage is not null)stage.Dispose();else ReleaseReadAdmission();}
        }

        private void RequireStoredProjectionCapabilities(BoundedPageRequest request)
        {
            var related=request.AnyRelatedFilters.Concat(request.RequiredRelatedGroups.SelectMany(group=>group.Alternatives));
            if(request.CopyFilters().Concat(request.CopyAnyFilters()).Concat(related.SelectMany(value=>value.CopyFilters().Concat(value.CopyAnyFilters())))
                .Any(filter=>filter.Operator=="equalsordinalignorecase")&&DataSource is not IOrdinalIgnoreCaseTextDataSource{SupportsOrdinalIgnoreCaseText:true})
                throw new NotSupportedException("The projection provider cannot enforce ordinal case-insensitive predicates.");
            if(request.RequiredRelatedGroups.Count>0&&DataSource is not IBoundedGroupedRelatedPagedDataSource||
                request.AnyRelatedFilters.Count>0&&DataSource is not IBoundedRelatedPagedDataSource||
                related.Any(value=>value.Comparisons.Count>0)&&DataSource is not IBoundedComparedRelatedPagedDataSource)
                throw new NotSupportedException("The projection provider cannot enforce all related predicates.");
        }

        private StoredProjectionPage DecodeStoredProjection(BoundedPageRequest request,BoundedPageResponse response,
            Dictionary<string,PropertyInfo> properties,string[] keys,CancellationToken token)
        {
            if(response is null||response.RequestId!=request.RequestId||response.PageNumber!=request.PageNumber||response.PageSize!=request.PageSize||response.TotalRecords<0||response.PayloadBytes>request.MaxPayloadBytes)
                throw new InvalidOperationException("The stored projection envelope is invalid.");
            _=new UTF8Encoding(false,true).GetCharCount(response.Payload.Span);
            using var document=JsonDocument.Parse(response.Payload,new JsonDocumentOptions{MaxDepth=3});
            var array=document.RootElement;
            var expected=request.Offset>=response.TotalRecords?0:(int)Math.Min(request.PageSize,response.TotalRecords-request.Offset);
            if(array.ValueKind!=JsonValueKind.Array||array.GetArrayLength()!=expected)throw new InvalidOperationException("Projection rows differ from the bounded snapshot count.");
            var rows=new List<StoredProjectionRow>(expected);var seenKeys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in array.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();if(row.ValueKind!=JsonValueKind.Object)throw new InvalidOperationException("Projection rows must be scalar objects.");
                var values=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
                foreach(var cell in row.EnumerateObject())
                {
                    if(!properties.TryGetValue(cell.Name,out var property)||values.ContainsKey(cell.Name)||cell.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                        throw new InvalidOperationException("Projection contains an unknown, duplicate or nonscalar column.");
                    if(cell.Value.ValueKind==JsonValueKind.Null&&property.PropertyType.IsValueType&&Nullable.GetUnderlyingType(property.PropertyType) is null)
                        throw new InvalidOperationException("Projection contains a null nonnullable scalar.");
                    values.Add(cell.Name,cell.Value.Deserialize(property.PropertyType));
                }
                if(values.Count!=properties.Count)throw new InvalidOperationException("Projection omitted selected fields.");
                if(IsTenantScoped&&TenantFiltersReads&&!string.Equals(Convert.ToString(values[TenantFieldName],CultureInfo.InvariantCulture),TenantId,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Projection returned a row outside the admitted tenant.");
                var keyText=new StringBuilder();
                foreach(var key in keys)
                {
                    var value=values[key]??throw new InvalidOperationException("Projection has a null primary key.");
                    var encoded=JsonSerializer.Serialize(value,properties[key].PropertyType);keyText.Append(encoded.Length).Append(':').Append(encoded);
                }
                if(!seenKeys.Add(keyText.ToString()))throw new InvalidOperationException("Projection contains duplicate primary keys.");
                rows.Add(new(values));
            }
            return new(new ProviderPageInfo(request,response.TotalRecords,rows.Count,response.PayloadBytes),rows);
        }
    }
}

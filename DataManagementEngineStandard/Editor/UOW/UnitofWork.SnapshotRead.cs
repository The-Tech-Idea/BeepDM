using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T>
    {
        /// <summary>Visits all rows privately in bounded batches from one native snapshot.
        /// Live Units are never replaced. A callback must not publish partial results or perform I/O.
        /// No complete result is acknowledged until the provider and all callbacks finish.</summary>
        public async Task<SnapshotReadCompletion> VisitSnapshotAsync(BoundedPageRequest request,
            Action<IReadOnlyList<T>> consume, CancellationToken cancellationToken=default)
        {
            ArgumentNullException.ThrowIfNull(request);ArgumentNullException.ThrowIfNull(consume);
            cancellationToken.ThrowIfCancellationRequested();
            if(request.PageNumber!=1)throw new ArgumentException("A complete traversal starts at page one.");
            if(Interlocked.CompareExchange(ref _commitAdmission,1,0)!=0)throw new InvalidOperationException("A commit or staged read is already active.");
            Volatile.Write(ref _readAdmission,1);
            ReadStage stage=null;
            Exception callbackFailure=null;int receiving=0,closed=0;
            ReadOutcome=new ErrorsInfo{Flag=Errors.Failed,Message="Snapshot traversal is not acknowledged."};
            try
            {
                if(!SupportsStagedPageRead || DataSource is not IBoundedSnapshotPagedDataSource source)
                    throw new NotSupportedException("This UoW/provider cannot admit a coherent snapshot traversal.");
                if(request.AnyRelatedFilters.Count>0 || request.RequiredRelatedGroups.Count>0)
                    throw new NotSupportedException("Related predicates are not qualified for this traversal boundary.");
                stage=new ReadStage(this,cancellationToken,true);stage.CheckTarget();
                var qualified=QualifyPageRequest(request);
                var args=new UnitofWorkParams{EventAction=EventAction.PreQuery};PreQuery?.Invoke(this,args);
                if(args.Cancel)throw new InvalidOperationException(args.Messege ?? "Snapshot traversal cancelled before dispatch.");
                stage.CheckTarget();
                var filters=qualified.CopyFilters();
                if(IsTenantScoped && TenantFiltersReads)filters.Add(new(){FieldName=TenantFieldName,Operator="=",FilterValue=TenantId});
                qualified=qualified.WithValues(qualified.Order,filters);
                long pages=0,visited=0;long? total=null;
                var completion=await source.VisitSnapshotPagesAsync(stage.EntityName,qualified,response=>
                {
                    if(Volatile.Read(ref closed)!=0 || Volatile.Read(ref callbackFailure)!=null || Interlocked.CompareExchange(ref receiving,1,0)!=0)
                    {
                        var refusal=new InvalidOperationException("Snapshot callbacks must be active, serial and failure-free.");
                        Interlocked.CompareExchange(ref callbackFailure,refusal,null);throw refusal;
                    }
                    try
                    {
                    stage.CheckTarget();
                    var batch=qualified.ForPage(checked(pages+1));
                    var rows=DecodePageResponse(batch,response,cancellationToken,out var info);
                    if(total is not null && total.Value!=info.TotalRecords)throw new InvalidOperationException("Snapshot count changed between batches.");
                    total=info.TotalRecords;
                    if(pages>0 && visited>=total)throw new InvalidOperationException("Unexpected extra snapshot batch.");
                    consume(rows.AsReadOnly());stage.CheckTarget();
                    visited=checked(visited+rows.Count);pages=checked(pages+1);
                    }
                    catch(Exception ex){Interlocked.CompareExchange(ref callbackFailure,ex,null);throw;}
                    finally{Volatile.Write(ref receiving,0);}
                },cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref closed,1);
                if(Volatile.Read(ref receiving)!=0 || callbackFailure!=null)
                    throw new InvalidOperationException("A snapshot callback failed or outlived producer completion.",callbackFailure);
                stage.CheckTarget();
                var expectedPages=total is null || total==0 ? 1 : 1+(total.Value-1)/qualified.PageSize;
                if(completion is null || completion.RequestId!=qualified.RequestId || total is null ||
                    completion.TotalRecords!=total || completion.RowsVisited!=visited || visited!=total ||
                    completion.PagesVisited!=pages || pages!=expectedPages)
                    throw new InvalidOperationException("Complete snapshot acknowledgement is missing or inconsistent.");
                ReadOutcome=new ErrorsInfo{Flag=Errors.Ok,Message="Complete coherent snapshot traversal acknowledged."};
                return completion;
            }
            catch(Exception ex)
            {Interlocked.CompareExchange(ref callbackFailure,ex,null);ReadOutcome=new ErrorsInfo{Flag=Errors.Failed,Message="Snapshot traversal did not complete.",Ex=ex};throw;}
            finally {Volatile.Write(ref closed,1);if(stage is not null)stage.Dispose();else ReleaseReadAdmission();}
        }
    }
}

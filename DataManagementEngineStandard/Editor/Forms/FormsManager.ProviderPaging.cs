using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        private static string PageKeySignature(IEnumerable<EntityField> fields) =>
            string.Join("|", (fields ?? Enumerable.Empty<EntityField>()).Select(field =>
                $"{field.FieldName?.Length ?? -1}:{field.FieldName}:{field.IsKey}"));

        private static void ConfigureManagedPage(ManagedReadPlan plan, BoundedPageRequest request)
        {
            var configuration = plan.Block.Configuration ?? throw new InvalidOperationException("Provider paging requires block configuration.");
            if (configuration.PageSize <= 0 || request.PageSize != configuration.PageSize ||
                configuration.MaxRecordsPerFetch <= 0 || request.PageSize > configuration.MaxRecordsPerFetch ||
                configuration.MaxRecords <= 0 || request.PageSize > configuration.MaxRecords)
                throw new ArgumentException("Provider page size must match enabled block paging and fit MaxRecordsPerFetch/MaxRecords.");
            if (!string.IsNullOrWhiteSpace(plan.Block.DefaultOrderByClause))
                throw new NotSupportedException("Provider paging requires explicit column order descriptors; raw DefaultOrderByClause is not supported.");
            var fields = plan.DeclaredFields;
            if (!fields.Any(field => field.IsKey)) throw new NotSupportedException("Provider paging requires complete declared primary keys.");
            var order = new List<PageOrder>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in request.Order)
            {
                var field = fields.SingleOrDefault(item => string.Equals(item.FieldName, column.FieldName, StringComparison.OrdinalIgnoreCase));
                if (field == null || !names.Add(column.FieldName))
                    throw new ArgumentException("Provider order fields must be unique declared column names.");
                order.Add(new PageOrder(field.FieldName, column.Descending));
            }
            foreach (var key in fields.Where(field => field.IsKey))
                if (names.Add(key.FieldName)) order.Add(new PageOrder(key.FieldName));
            plan.PageConfiguration = configuration;
            plan.PageSize = configuration.PageSize;
            plan.PageMaxFetch = configuration.MaxRecordsPerFetch;
            plan.PageMaxRecords = configuration.MaxRecords;
            plan.PageKeys = PageKeySignature(fields);
            plan.PageDefaultOrder = plan.Block.DefaultOrderByClause;
            plan.PageRequest = request.WithValues(order, plan.Filters);
        }

        // Only owned configuration properties are read at the final publication checkpoint.
        private static void VerifyPageConfiguration(ManagedReadPlan plan)
        {
            var configuration = plan.PageConfiguration;
            if (!ReferenceEquals(configuration, plan.Block.Configuration) ||
                configuration.PageSize != plan.PageSize || configuration.MaxRecordsPerFetch != plan.PageMaxFetch ||
                configuration.MaxRecords != plan.PageMaxRecords || plan.Block.DefaultOrderByClause != plan.PageDefaultOrder)
                throw new InvalidOperationException("Provider page configuration changed during the operation.");
        }
    }
}

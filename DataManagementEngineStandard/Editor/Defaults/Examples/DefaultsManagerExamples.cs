using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Defaults.Interfaces;

namespace TheTechIdea.Beep.Editor.Defaults.Examples
{
    /// <summary>
    /// Comprehensive examples of using the Enhanced DefaultsManager
    /// </summary>
    public static class DefaultsManagerExamples
    {
        /// <summary>
        /// Basic usage examples
        /// </summary>
        public static void BasicUsageExamples(IDMEEditor editor)
        {
            Console.WriteLine("=== Basic Usage Examples ===");

            // Initialize the DefaultsManager (should be done once at startup)
            DefaultsManager.Initialize(editor);

            // Example 1: Set static default values
            Console.WriteLine("1. Setting static default values...");
            
            var result = DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "Status", "Active");
            Console.WriteLine($"   Status field: {result.Message}");
            
            result = DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "IsDeleted", "false");
            Console.WriteLine($"   IsDeleted field: {result.Message}");

            // Example 2: Set dynamic rule-based defaults
            Console.WriteLine("\n2. Setting dynamic rule-based defaults...");
            
            result = DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedBy", "USERNAME", isRule: true);
            Console.WriteLine($"   CreatedBy field: {result.Message}");
            
            result = DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate", "NOW", isRule: true);
            Console.WriteLine($"   CreatedDate field: {result.Message}");
            
            result = DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "UserID", "NEWGUID", isRule: true);
            Console.WriteLine($"   UserID field: {result.Message}");

            // Example 3: Get column defaults
            Console.WriteLine("\n3. Getting column defaults...");
            
            var defaultValue = DefaultsManager.GetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate");
            Console.WriteLine($"   CreatedDate default resolved to: {defaultValue}");
            
            defaultValue = DefaultsManager.GetColumnDefault(editor, "MyDatabase", "Users", "Status");
            Console.WriteLine($"   Status default resolved to: {defaultValue}");

            // Example 4: Get all defaults for an entity
            Console.WriteLine("\n4. Getting all defaults for Users entity...");
            
            var entityDefaults = DefaultsManager.GetEntityDefaults(editor, "MyDatabase", "Users");
            foreach (var def in entityDefaults)
            {
                Console.WriteLine($"   {def.Key}: Value='{def.Value.PropertyValue}', Rule='{def.Value.Rule}'");
            }
        }

        /// <summary>
        /// Advanced rule examples
        /// </summary>
        public static void AdvancedRuleExamples(IDMEEditor editor)
        {
            Console.WriteLine("\n=== Advanced Rule Examples ===");

            // Example 1: DateTime rules
            Console.WriteLine("1. DateTime rules...");
            TestAndSetRule(editor, "Orders", "OrderDate", "TODAY");
            TestAndSetRule(editor, "Orders", "ExpiryDate", "ADDDAYS(TODAY, 30)");
            TestAndSetRule(editor, "Orders", "FormattedDate", "FORMAT(NOW, 'yyyy-MM-dd HH:mm:ss')");

            // Example 2: User context rules
            Console.WriteLine("\n2. User context rules...");
            TestAndSetRule(editor, "AuditLog", "ActionBy", "USERNAME");
            TestAndSetRule(editor, "Sessions", "UserEmail", "USEREMAIL");
            TestAndSetRule(editor, "Requests", "OriginMachine", "MACHINENAME");

            // Example 3: GUID and sequence rules
            Console.WriteLine("\n3. GUID and sequence rules...");
            TestAndSetRule(editor, "Products", "ProductID", "NEWGUID");
            TestAndSetRule(editor, "Orders", "OrderNumber", "SEQUENCE(10000)");
            TestAndSetRule(editor, "Customers", "CustomerCode", "GUID(N)");

            // Example 4: Formula rules
            Console.WriteLine("\n4. Formula rules...");
            TestAndSetRule(editor, "TestData", "RandomScore", "RANDOM(70, 100)");
            TestAndSetRule(editor, "Inventory", "NextReorderID", "INCREMENT(ReorderID)");
        }

        /// <summary>
        /// Bulk operations examples
        /// </summary>
        public static void BulkOperationsExamples(IDMEEditor editor)
        {
            Console.WriteLine("\n=== Bulk Operations Examples ===");

            // Example 1: Set multiple defaults at once
            Console.WriteLine("1. Setting multiple defaults for Orders entity...");
            
            var columnDefaults = new Dictionary<string, (string value, bool isRule)>
            {
                { "CreatedBy", ("USERNAME", true) },
                { "CreatedDate", ("NOW", true) },
                { "ModifiedBy", ("USERNAME", true) },
                { "ModifiedDate", ("NOW", true) },
                { "Status", ("Pending", false) },
                { "Priority", ("Normal", false) },
                { "IsDeleted", ("false", false) },
                { "OrderID", ("NEWGUID", true) }
            };

            var result = DefaultsManager.SetMultipleColumnDefaults(editor, "MyDatabase", "Orders", columnDefaults);
            Console.WriteLine($"   Result: {result.Message}");

            // Example 2: Apply defaults to a record
            Console.WriteLine("\n2. Applying defaults to a new record...");
            
            var order = new SampleOrder();
            Console.WriteLine($"   Before: ID={order.OrderID}, CreatedBy='{order.CreatedBy}', Status='{order.Status}'");
            
            order = DefaultsManager.ApplyDefaultsToRecord(editor, "MyDatabase", "Orders", order) as SampleOrder;
            Console.WriteLine($"   After:  ID={order.OrderID}, CreatedBy='{order.CreatedBy}', Status='{order.Status}'");
        }

        /// <summary>
        /// Template examples
        /// </summary>
        public static void TemplateExamples(IDMEEditor editor)
        {
            Console.WriteLine("\n=== Template Examples ===");

            // Example 1: Audit fields template
            Console.WriteLine("1. Creating audit fields template...");
            var auditFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.AuditFields);
            foreach (var field in auditFields)
            {
                Console.WriteLine($"   {field.PropertyName}: Value='{field.PropertyValue}', Rule='{field.Rule}'");
            }

            // Example 2: System fields template
            Console.WriteLine("\n2. Creating system fields template...");
            var systemFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.SystemFields);
            foreach (var field in systemFields)
            {
                Console.WriteLine($"   {field.PropertyName}: Value='{field.PropertyValue}', Rule='{field.Rule}'");
            }

            // Example 3: Common defaults template
            Console.WriteLine("\n3. Creating common defaults template...");
            var commonDefaults = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.CommonDefaults);
            foreach (var field in commonDefaults)
            {
                Console.WriteLine($"   {field.PropertyName}: Value='{field.PropertyValue}', Rule='{field.Rule}'");
            }
        }

        /// <summary>
        /// Custom resolver example
        /// </summary>
        public static void CustomResolverExample(IDMEEditor editor)
        {
            Console.WriteLine("\n=== Custom Resolver Example ===");

            // Register a custom business logic resolver
            var customResolver = new BusinessLogicResolver(editor);
            DefaultsManager.RegisterCustomResolver(editor, customResolver);

            Console.WriteLine("1. Registered custom business logic resolver");

            // Test custom rules
            Console.WriteLine("\n2. Testing custom rules...");
            TestAndSetRule(editor, "Orders", "OrderNumber", "NEXTORDERNUM");
            TestAndSetRule(editor, "Customers", "CustomerCode", "NEWCUSTOMERCODE");
            TestAndSetRule(editor, "Products", "SKU", "GENERATESKU");
        }

        /// <summary>
        /// Validation examples
        /// </summary>
        public static void ValidationExamples(IDMEEditor editor)
        {
            Console.WriteLine("\n=== Validation Examples ===");

            // Example 1: Test valid rules
            Console.WriteLine("1. Testing valid rules...");
            TestRule(editor, "NOW");
            TestRule(editor, "USERNAME");
            TestRule(editor, "ADDDAYS(TODAY, 7)");
            TestRule(editor, "NEWGUID");

            // Example 2: Test invalid rules
            Console.WriteLine("\n2. Testing invalid rules...");
            TestRule(editor, "INVALID_RULE");
            TestRule(editor, "ADDDAYS(TODAY");  // Missing closing parenthesis
            TestRule(editor, "RANDOM(abc, def)");  // Invalid parameters
        }

        /// <summary>
        /// Import/Export examples
        /// </summary>
        public static void ImportExportExamples(IDMEEditor editor)
        {
            Console.WriteLine("\n=== Import/Export Examples ===");

            // Example 1: Export current defaults
            Console.WriteLine("1. Exporting defaults...");
            var exportedConfig = DefaultsManager.ExportDefaults(editor, "MyDatabase");
            Console.WriteLine($"   Exported {exportedConfig?.Length ?? 0} characters of configuration");

            // Example 2: Import defaults (demo only - would need actual file)
            Console.WriteLine("\n2. Import example (would need actual configuration file)");
            // var importResult = DefaultsManager.ImportDefaults(editor, "TestDatabase", exportedConfig, false);
            // Console.WriteLine($"   Import result: {importResult.Message}");
        }

        /// <summary>
        /// Helper method to test and set a rule
        /// </summary>
        private static void TestAndSetRule(IDMEEditor editor, string entityName, string columnName, string rule)
        {
            // Test the rule first
            var (testResult, testValue) = DefaultsManager.TestRule(editor, rule);
            
            if (testResult.Flag == Errors.Ok)
            {
                // Rule is valid, set it as default
                var setResult = DefaultsManager.SetColumnDefault(editor, "MyDatabase", entityName, columnName, rule, isRule: true);
                Console.WriteLine($"   {entityName}.{columnName} -> '{rule}' = {testValue} ({setResult.Flag})");
            }
            else
            {
                Console.WriteLine($"   {entityName}.{columnName} -> '{rule}' FAILED: {testResult.Message}");
            }
        }

        /// <summary>
        /// Helper method to test a rule
        /// </summary>
        private static void TestRule(IDMEEditor editor, string rule)
        {
            var (testResult, testValue) = DefaultsManager.TestRule(editor, rule);
            
            if (testResult.Flag == Errors.Ok)
            {
                Console.WriteLine($"   '{rule}' -> {testValue} ?");
            }
            else
            {
                Console.WriteLine($"   '{rule}' -> FAILED: {testResult.Message} ?");
            }
        }

        /// <summary>
        /// Display available resolvers and their capabilities
        /// </summary>
        public static void ShowAvailableResolvers(IDMEEditor editor)
        {
            Console.WriteLine("\n=== Available Resolvers ===");

            var resolvers = DefaultsManager.GetAvailableResolvers(editor);
            var examples = DefaultsManager.GetResolverExamples(editor);

            foreach (var resolver in resolvers)
            {
                Console.WriteLine($"\n{resolver.Key} Resolver:");
                Console.WriteLine("  Supported Rules:");
                foreach (var ruleType in resolver.Value)
                {
                    Console.WriteLine($"    - {ruleType}");
                }

                if (examples.ContainsKey(resolver.Key))
                {
                    Console.WriteLine("  Examples:");
                    foreach (var example in examples[resolver.Key])
                    {
                        Console.WriteLine($"    {example}");
                    }
                }
            }
        }

        /// <summary>
        /// Run all examples
        /// </summary>
        public static void RunAllExamples(IDMEEditor editor)
        {
            try
            {
                Console.WriteLine("Enhanced DefaultsManager Examples");
                Console.WriteLine("=================================");

                BasicUsageExamples(editor);
                AdvancedRuleExamples(editor);
                BulkOperationsExamples(editor);
                TemplateExamples(editor);
                CustomResolverExample(editor);
                ValidationExamples(editor);
                ImportExportExamples(editor);
                ShowAvailableResolvers(editor);

                Console.WriteLine("\n=== Examples Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running examples: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sample order class for examples
    /// </summary>
    public class SampleOrder
    {
        public string OrderID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public bool IsDeleted { get; set; }
    }

    /// <summary>
    /// Custom business logic resolver example
    /// </summary>
    public class BusinessLogicResolver : IDefaultValueResolver
    {
        private readonly IDMEEditor _editor;
        private static int _orderCounter = 10000;
        private static int _customerCounter = 1000;
        private static int _skuCounter = 1;

        public BusinessLogicResolver(IDMEEditor editor)
        {
            _editor = editor;
        }

        public string ResolverName => "BusinessLogic";

        public IEnumerable<string> SupportedRuleTypes => new[]
        {
            "NEXTORDERNUM", "NEWCUSTOMERCODE", "GENERATESKU"
        };

        public object ResolveValue(string rule, IPassedArgs parameters)
        {
            return rule.ToUpperInvariant().Trim() switch
            {
                "NEXTORDERNUM" => GenerateNextOrderNumber(),
                "NEWCUSTOMERCODE" => GenerateCustomerCode(),
                "GENERATESKU" => GenerateProductSKU(),
                _ => null
            };
        }

        public bool CanHandle(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            var upperRule = rule.ToUpperInvariant().Trim();
            return SupportedRuleTypes.Any(type => upperRule.Contains(type));
        }

        public IEnumerable<string> GetExamples()
        {
            return new[]
            {
                "NEXTORDERNUM - Generate next order number (ORD-20240115-10001)",
                "NEWCUSTOMERCODE - Generate customer code (CUST-20240115-ABC123)",
                "GENERATESKU - Generate product SKU (SKU-000001)"
            };
        }

        private string GenerateNextOrderNumber()
        {
            _orderCounter++;
            return $"ORD-{DateTime.Now:yyyyMMdd}-{_orderCounter:D5}";
        }

        private string GenerateCustomerCode()
        {
            _customerCounter++;
            var randomSuffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"CUST-{DateTime.Now:yyMMdd}-{randomSuffix}";
        }

        private string GenerateProductSKU()
        {
            _skuCounter++;
            return $"SKU-{_skuCounter:D6}";
        }
    }
}
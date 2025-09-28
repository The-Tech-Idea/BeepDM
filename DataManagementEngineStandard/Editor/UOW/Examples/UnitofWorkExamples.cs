using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOW.Examples
{
    /// <summary>
    /// Examples demonstrating usage of the refactored UnitofWork with DefaultsManager integration
    /// </summary>
    public class UnitofWorkExamples
    {
        #region Example Entity Class

        /// <summary>
        /// Example entity for demonstration purposes
        /// </summary>
        public class Customer : Entity
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public DateTime CreatedDate { get; set; }
            public string CreatedBy { get; set; }
            public DateTime? ModifiedDate { get; set; }
            public string ModifiedBy { get; set; }
            public bool IsActive { get; set; }
            public string Status { get; set; }
        }

        #endregion

        #region Basic UnitofWork Usage

        /// <summary>
        /// Demonstrates basic UnitofWork initialization and usage
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task<bool> BasicUsageExample(IDMEEditor editor)
        {
            try
            {
                // Initialize UnitofWork for Customer entity
                using var unitOfWork = new UnitofWork<Customer>(
                    editor, 
                    "MyDatabase", 
                    "Customers", 
                    "ID"
                );

                // Load data
                var customers = await unitOfWork.Get();
                Console.WriteLine($"Loaded {customers.Count} customers");

                // Create a new customer - default values will be automatically applied
                unitOfWork.New();
                var newCustomer = unitOfWork.CurrentItem;
                newCustomer.Name = "John Doe";
                newCustomer.Email = "john.doe@example.com";
                // CreatedDate, CreatedBy, IsActive, Status will be set by DefaultsManager

                // Update an existing customer - update defaults will be applied
                if (customers.Count > 0)
                {
                    var firstCustomer = customers[0];
                    firstCustomer.Name = "Updated Name";
                    // ModifiedDate, ModifiedBy will be set by DefaultsManager
                    unitOfWork.Update(firstCustomer);
                }

                // Commit all changes
                var result = await unitOfWork.Commit();
                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BasicUsageExample: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region DefaultsManager Integration Example

        /// <summary>
        /// Demonstrates how to configure and use DefaultsManager with UnitofWork
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task<bool> DefaultsManagerIntegrationExample(IDMEEditor editor)
        {
            try
            {
                // Initialize DefaultsManager (this would typically be done once at application startup)
                DefaultsManager.Initialize(editor);

                // Configure default values for Customer entity
                await ConfigureCustomerDefaults(editor, "MyDatabase");

                // Initialize UnitofWork - it will automatically use the configured defaults
                using var unitOfWork = new UnitofWork<Customer>(
                    editor, 
                    "MyDatabase", 
                    "Customers", 
                    "ID"
                );

                // Create a new customer - defaults will be automatically applied
                unitOfWork.New();
                var newCustomer = unitOfWork.CurrentItem;
                newCustomer.Name = "Jane Smith";
                newCustomer.Email = "jane.smith@example.com";
                
                // Verify that defaults were applied
                Console.WriteLine($"Created Date: {newCustomer.CreatedDate}");
                Console.WriteLine($"Created By: {newCustomer.CreatedBy}");
                Console.WriteLine($"Is Active: {newCustomer.IsActive}");
                Console.WriteLine($"Status: {newCustomer.Status}");

                // Add another customer
                var anotherCustomer = new Customer
                {
                    Name = "Bob Johnson",
                    Email = "bob.johnson@example.com"
                };
                unitOfWork.Add(anotherCustomer);
                // Defaults will be applied automatically

                // Update a customer - update defaults will be applied
                newCustomer.Name = "Jane Smith Updated";
                unitOfWork.Update(newCustomer);
                
                // Verify update defaults were applied
                Console.WriteLine($"Modified Date: {newCustomer.ModifiedDate}");
                Console.WriteLine($"Modified By: {newCustomer.ModifiedBy}");

                // Commit changes
                var result = await unitOfWork.Commit();
                Console.WriteLine($"Commit result: {result.Flag} - {result.Message}");

                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DefaultsManagerIntegrationExample: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configures default values for Customer entity
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <param name="dataSourceName">Data source name</param>
        /// <returns>Task representing the async operation</returns>
        private static async Task ConfigureCustomerDefaults(IDMEEditor editor, string dataSourceName)
        {
            try
            {
                // Set up audit field defaults
                DefaultsManager.SetColumnDefault(editor, dataSourceName, "Customers", 
                    "CreatedDate", "NOW", isRule: true);
                
                DefaultsManager.SetColumnDefault(editor, dataSourceName, "Customers", 
                    "CreatedBy", "USERNAME", isRule: true);
                
                DefaultsManager.SetColumnDefault(editor, dataSourceName, "Customers", 
                    "ModifiedDate", "NOW", isRule: true);
                
                DefaultsManager.SetColumnDefault(editor, dataSourceName, "Customers", 
                    "ModifiedBy", "USERNAME", isRule: true);

                // Set up static defaults (FilterValue is object, but ensure string inputs in examples)
                DefaultsManager.SetColumnDefault(editor, dataSourceName, "Customers", 
                    "IsActive", "true", isRule: false);
                
                DefaultsManager.SetColumnDefault(editor, dataSourceName, "Customers", 
                    "Status", "Active", isRule: false);

                Console.WriteLine("Customer defaults configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring customer defaults: {ex.Message}");
            }
        }

        #endregion

        #region List Mode Example

        /// <summary>
        /// Demonstrates UnitofWork usage in list mode with in-memory data
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task<bool> ListModeExample(IDMEEditor editor)
        {
            try
            {
                // Create initial data
                var initialCustomers = new ObservableBindingList<Customer>
                {
                    new Customer { ID = 1, Name = "Customer 1", Email = "customer1@example.com" },
                    new Customer { ID = 2, Name = "Customer 2", Email = "customer2@example.com" }
                };

                // Initialize UnitofWork in list mode
                using var unitOfWork = new UnitofWork<Customer>(
                    editor, 
                    true, // isInListMode 
                    initialCustomers, 
                    "ID"
                );

                Console.WriteLine($"Initial count: {unitOfWork.Units.Count}");

                // Add a new customer - defaults will be applied
                unitOfWork.New();
                var newCustomer = unitOfWork.CurrentItem;
                newCustomer.ID = 3;
                newCustomer.Name = "Customer 3";
                newCustomer.Email = "customer3@example.com";

                Console.WriteLine($"After adding new customer: {unitOfWork.Units.Count}");

                // Update an existing customer
                var firstCustomer = unitOfWork.Units[0];
                firstCustomer.Name = "Updated Customer 1";
                unitOfWork.Update(firstCustomer);

                // Delete a customer using the typed overload
                unitOfWork.Delete(unitOfWork.Units[1]);

                Console.WriteLine($"After deletion: {unitOfWork.Units.Count}");
                Console.WriteLine($"Is dirty: {unitOfWork.IsDirty}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ListModeExample: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Filtering and Paging Example

        /// <summary>
        /// Demonstrates filtering and paging capabilities
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task<bool> FilteringAndPagingExample(IDMEEditor editor)
        {
            try
            {
                using var unitOfWork = new UnitofWork<Customer>(
                    editor, 
                    "MyDatabase", 
                    "Customers", 
                    "ID"
                );

                // Load all data first
                var allCustomers = await unitOfWork.Get();
                Console.WriteLine($"Total customers: {allCustomers.Count}");

                // Apply filters - pass values as strings for consistency
                var filters = new List<AppFilter>
                {
                    new AppFilter 
                    { 
                        FieldName = "IsActive", 
                        Operator = "=", 
                        FilterValue = "true" 
                    },
                    new AppFilter 
                    { 
                        FieldName = "Status", 
                        Operator = "=", 
                        FilterValue = "Active" 
                    }
                };

                var filteredCustomers = await unitOfWork.Get(filters);
                Console.WriteLine($"Filtered customers: {filteredCustomers.Count}");

                // Apply paging
                unitOfWork.PageIndex = 0;
                unitOfWork.PageSize = 10;
                
                var pagedFilters = filters.Concat(new[]
                {
                    new AppFilter { FieldName = "PageIndex", FilterValue = unitOfWork.PageIndex.ToString() },
                    new AppFilter { FieldName = "PageSize", FilterValue = unitOfWork.PageSize.ToString() }
                }).ToList();

                var pagedCustomers = await unitOfWork.Get(pagedFilters);
                Console.WriteLine($"Paged customers: {pagedCustomers.Count}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FilteringAndPagingExample: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Error Handling Example

        /// <summary>
        /// Demonstrates error handling and validation
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task<bool> ErrorHandlingExample(IDMEEditor editor)
        {
            try
            {
                using var unitOfWork = new UnitofWork<Customer>(
                    editor, 
                    "MyDatabase", 
                    "Customers", 
                    "ID"
                );

                // Try to update a non-existent customer
                var nonExistentCustomer = new Customer
                {
                    ID = -1,
                    Name = "Non-existent",
                    Email = "nonexistent@example.com"
                };

                var updateResult = unitOfWork.Update(nonExistentCustomer);
                Console.WriteLine($"Update result: {updateResult.Flag} - {updateResult.Message}");

                // Try to delete a non-existent customer (typed overload)
                var deleteResult = unitOfWork.Delete(nonExistentCustomer);
                Console.WriteLine($"Delete result: {deleteResult.Flag} - {deleteResult.Message}");

                // Validate a customer before operations
                unitOfWork.New();
                var newCustomer = unitOfWork.CurrentItem;
                // Leave required fields empty to trigger validation errors

                var commitResult = await unitOfWork.Commit();
                Console.WriteLine($"Commit result: {commitResult.Flag} - {commitResult.Message}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ErrorHandlingExample: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Event Handling Example

        /// <summary>
        /// Demonstrates event handling in UnitofWork
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task<bool> EventHandlingExample(IDMEEditor editor)
        {
            try
            {
                using var unitOfWork = new UnitofWork<Customer>(
                    editor, 
                    "MyDatabase", 
                    "Customers", 
                    "ID"
                );

                // Subscribe to events
                unitOfWork.PreInsert += (sender, args) =>
                {
                    Console.WriteLine($"Pre-Insert: Processing {args.EventAction}");
                    // You can cancel the operation by setting args.Cancel = true
                };

                unitOfWork.PostInsert += (sender, args) =>
                {
                    Console.WriteLine($"Post-Insert: Completed {args.EventAction}");
                };

                unitOfWork.PreUpdate += (sender, args) =>
                {
                    Console.WriteLine($"Pre-Update: Processing {args.EventAction}");
                };

                unitOfWork.PostUpdate += (sender, args) =>
                {
                    Console.WriteLine($"Post-Update: Completed {args.EventAction}");
                };

                unitOfWork.PreDelete += (sender, args) =>
                {
                    Console.WriteLine($"Pre-Delete: Processing {args.EventAction}");
                };

                unitOfWork.PostDelete += (sender, args) =>
                {
                    Console.WriteLine($"Post-Delete: Completed {args.EventAction}");
                };

                // Perform operations - events will be triggered
                unitOfWork.New();
                var newCustomer = unitOfWork.CurrentItem;
                newCustomer.Name = "Event Test Customer";
                newCustomer.Email = "eventtest@example.com";

                var customers = await unitOfWork.Get();
                if (customers.Count > 0)
                {
                    var firstCustomer = customers[0];
                    firstCustomer.Name = "Updated via Events";
                    unitOfWork.Update(firstCustomer);
                }

                var commitResult = await unitOfWork.Commit();
                Console.WriteLine($"Commit with events result: {commitResult.Flag}");

                return commitResult.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EventHandlingExample: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Performance Example

        /// <summary>
        /// Demonstrates performance considerations and batch operations
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task<bool> PerformanceExample(IDMEEditor editor)
        {
            try
            {
                using var unitOfWork = new UnitofWork<Customer>(
                    editor, 
                    "MyDatabase", 
                    "Customers", 
                    "ID"
                );

                var startTime = DateTime.Now;

                // Batch insert operations
                for (int i = 0; i < 100; i++)
                {
                    var customer = new Customer
                    {
                        Name = $"Batch Customer {i}",
                        Email = $"batch{i}@example.com"
                    };
                    unitOfWork.Add(customer);
                }

                Console.WriteLine($"Added 100 customers in {(DateTime.Now - startTime).TotalMilliseconds}ms");

                // Commit all at once for better performance
                startTime = DateTime.Now;
                var result = await unitOfWork.Commit();
                Console.WriteLine($"Committed 100 customers in {(DateTime.Now - startTime).TotalMilliseconds}ms");

                // Check if dirty tracking is working
                Console.WriteLine($"Is dirty after commit: {unitOfWork.IsDirty}");

                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PerformanceExample: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Main Demo Method

        /// <summary>
        /// Runs all examples
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task RunAllExamples(IDMEEditor editor)
        {
            Console.WriteLine("=== UnitofWork Refactored Examples ===\n");

            Console.WriteLine("1. Running Basic Usage Example...");
            await BasicUsageExample(editor);
            Console.WriteLine();

            Console.WriteLine("2. Running DefaultsManager Integration Example...");
            await DefaultsManagerIntegrationExample(editor);
            Console.WriteLine();

            Console.WriteLine("3. Running List Mode Example...");
            await ListModeExample(editor);
            Console.WriteLine();

            Console.WriteLine("4. Running Filtering and Paging Example...");
            await FilteringAndPagingExample(editor);
            Console.WriteLine();

            Console.WriteLine("5. Running Error Handling Example...");
            await ErrorHandlingExample(editor);
            Console.WriteLine();

            Console.WriteLine("6. Running Event Handling Example...");
            await EventHandlingExample(editor);
            Console.WriteLine();

            Console.WriteLine("7. Running Performance Example...");
            await PerformanceExample(editor);
            Console.WriteLine();

            Console.WriteLine("=== All Examples Completed ===");
        }

        #endregion
    }
}
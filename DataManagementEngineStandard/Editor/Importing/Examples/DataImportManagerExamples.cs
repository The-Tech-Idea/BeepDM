using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Importing.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Defaults;

namespace TheTechIdea.Beep.Editor.Importing.Examples
{
    /// <summary>
    /// Comprehensive examples demonstrating the enhanced DataImportManager functionality
    /// </summary>
    public class DataImportManagerExamples
    {
        private readonly IDMEEditor _editor;

        public DataImportManagerExamples(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        #region Basic Usage Examples

        /// <summary>
        /// Example 1: Simple data import with backward compatibility
        /// </summary>
        public async Task<bool> Example1_SimpleDataImport()
        {
            Console.WriteLine("=== Example 1: Simple Data Import ===");

            using var importManager = new DataImportManager(_editor);

            try
            {
                // Configure import using backward-compatible properties
                importManager.SourceEntityName = "SourceCustomers";
                importManager.SourceDataSourceName = "ExternalCRM";
                importManager.DestEntityName = "Customers";
                importManager.DestDataSourceName = "MainDatabase";

                // Load destination entity structure
                var loadResult = importManager.LoadDestEntityStructure("Customers", "MainDatabase");
                if (loadResult.Flag != Errors.Ok)
                {
                    Console.WriteLine($"Failed to load entity structure: {loadResult.Message}");
                    return false;
                }

                // Set up progress reporting for backward-compatible API
                IProgress<IPassedArgs> progress = new Progress<IPassedArgs>(args =>
                {
                    Console.WriteLine($"Progress: {args.Messege} - Records: {args.ParameterInt1}");
                });

                // Execute import
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
                var result = await importManager.RunImportAsync(progress, cts.Token, null, 100);

                Console.WriteLine($"Import Result: {result.Message}");
                Console.WriteLine($"Total log entries: {importManager.ImportLogData.Count}");

                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Example 2: Enhanced import with configuration object
        /// </summary>
        public async Task<bool> Example2_EnhancedConfigurationImport()
        {
            Console.WriteLine("\n=== Example 2: Enhanced Configuration Import ===");

            using var importManager = new DataImportManager(_editor);

            try
            {
                // Create enhanced configuration
                var config = importManager.CreateImportConfiguration(
                    "ProductsExport", "ExternalSystem",
                    "Products", "MainDatabase");

                // Add filters for incremental import
                config.SourceFilters.Add(new AppFilter
                {
                   FieldName = "ModifiedDate",
                    Operator = ">=",
                    FilterValue = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd")
                });

                // Select specific fields
                config.SelectedFields = new List<string>
                {
                    "ProductCode", "ProductName", "Price", "Category", "ModifiedDate"
                };

                // Configure batch processing
                config.BatchSize = 200;
                config.CreateDestinationIfNotExists = true;
                config.ApplyDefaults = true;

                // Custom transformation
                config.CustomTransformation = (record) =>
                {
                    if (record is Dictionary<string, object> dict)
                    {
                        // Add audit fields
                        dict["ImportedDate"] = DateTime.Now;
                        dict["ImportedBy"] = Environment.UserName;
                        
                        // Normalize price
                        if (dict.ContainsKey("Price") && dict["Price"] != null)
                        {
                            if (decimal.TryParse(dict["Price"].ToString(), out var price))
                            {
                                dict["Price"] = Math.Round(price, 2);
                            }
                        }
                    }
                    return record;
                };

                // Set configuration
                var configResult = importManager.SetImportConfiguration(config);
                if (configResult.Flag != Errors.Ok)
                {
                    Console.WriteLine($"Configuration failed: {configResult.Message}");
                    return false;
                }

                // Enhanced progress reporting with metrics
                var startTime = DateTime.Now;
                IProgress<IPassedArgs> progress = new Progress<IPassedArgs>(args =>
                {
                    if (args.ParameterInt2 > 0) // Total records available
                    {
                        var percentage = (args.ParameterInt1 * 100.0) / args.ParameterInt2;
                        var elapsed = DateTime.Now - startTime;
                        var rate = elapsed.TotalSeconds > 0 ? args.ParameterInt1 / elapsed.TotalSeconds : 0;

                        Console.WriteLine($"Progress: {percentage:F1}% ({args.ParameterInt1}/{args.ParameterInt2}) " +
                                        $"Rate: {rate:F1} records/sec - {args.Messege}");
                    }
                    else
                    {
                        Console.WriteLine($"Progress: {args.Messege} - Records processed: {args.ParameterInt1}");
                    }
                });

                // Execute enhanced import
                using var cts = new CancellationTokenSource(TimeSpan.FromHours(1));
                var result = await importManager.RunImportAsync(config, progress, cts.Token);

                // Display results
                Console.WriteLine($"\nImport Result: {result.Message}");
                
                // Build summary from ImportLogData to avoid relying on concrete helper interface
                var logs = importManager.ImportLogData;
                var total = logs.Count;
                var errors = logs.Count(l => l.Level == ImportLogLevel.Error);
                var warnings = logs.Count(l => l.Level == ImportLogLevel.Warning);
                var duration = total > 0 ? (logs[^1].Timestamp - logs[0].Timestamp) : TimeSpan.Zero;

                Console.WriteLine("Log Summary:");
                Console.WriteLine($"  Total Entries: {total}");
                Console.WriteLine($"  Errors: {errors}");
                Console.WriteLine($"  Warnings: {warnings}");
                Console.WriteLine($"  Duration: {duration:hh\\:mm\\:ss}");

                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Advanced Usage Examples

        /// <summary>
        /// Example 3: Import with validation and testing
        /// </summary>
        public async Task<bool> Example3_ValidatedImport()
        {
            Console.WriteLine("\n=== Example 3: Validated Import ===");

            using var importManager = new DataImportManager(_editor);

            try
            {
                // Create configuration
                var config = new DataImportConfiguration
                {
                    SourceEntityName = "OrdersData",
                    SourceDataSourceName = "OrderSystem",
                    DestEntityName = "Orders",
                    DestDataSourceName = "DataWarehouse",
                    BatchSize = 150
                };

                // Test configuration before running import
                Console.WriteLine("Testing import configuration...");
                var testResult = await importManager.TestImportConfigurationAsync(config);
                
                if (testResult.Flag != Errors.Ok)
                {
                    Console.WriteLine($"Configuration test failed: {testResult.Message}");
                    return false;
                }

                Console.WriteLine("Configuration test passed. Proceeding with import...");

                // Validate specific aspects
                var validation = importManager.ValidationHelper.ValidateImportConfiguration(config);
                if (validation.Flag != Errors.Ok)
                {
                    Console.WriteLine($"Validation failed: {validation.Message}");
                    return false;
                }

                // Set up comprehensive progress monitoring
                var progressTracker = new ImportProgressTracker();
                IProgress<IPassedArgs> progress = new Progress<IPassedArgs>(args =>
                {
                    // Convert interface args to PassedArgs for the tracker
                    var pa = args as PassedArgs ?? new PassedArgs
                    {
                        Messege = args.Messege,
                        ParameterInt1 = args.ParameterInt1,
                        ParameterInt2 = args.ParameterInt2
                    };
                    progressTracker.ReportProgress(pa);
                });

                // Execute import with validation
                using var cts = new CancellationTokenSource(TimeSpan.FromHours(2));
                var result = await importManager.RunImportAsync(config, progress, cts.Token);

                // Display comprehensive results
                Console.WriteLine($"\nValidated Import Result: {result.Message}");
                progressTracker.DisplaySummary();

                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Example 4: Import with custom helpers and advanced features
        /// </summary>
        public async Task<bool> Example4_AdvancedFeaturesImport()
        {
            Console.WriteLine("\n=== Example 4: Advanced Features Import ===");

            using var importManager = new DataImportManager(_editor);

            try
            {
                // Create advanced configuration
                var config = new DataImportConfiguration
                {
                    SourceEntityName = "EmployeeExport",
                    SourceDataSourceName = "HRSystem",
                    DestEntityName = "Employees",
                    DestDataSourceName = "CoreDatabase"
                };

                // Use helper methods directly for fine-grained control
                Console.WriteLine("Using validation helper...");
                var entityValidation = importManager.ValidationHelper.ValidateImportConfiguration(config);
                Console.WriteLine($"Validation result: {entityValidation.Message}");

                // Calculate optimal batch size
                Console.WriteLine("Calculating optimal batch size...");
                var optimalBatchSize = importManager.BatchHelper.CalculateOptimalBatchSize(
                    totalRecords: 50000,
                    estimatedRecordSize: 2048, // 2KB per record
                    availableMemory: 100 * 1024 * 1024 // 100MB available
                );
                
                config.BatchSize = optimalBatchSize;
                Console.WriteLine($"Optimal batch size: {optimalBatchSize}");

                // Advanced transformation pipeline
                config.CustomTransformation = (record) =>
                {
                    // Use transformation helper for complex operations
                    var transformedRecord = importManager.TransformationHelper.ApplyTransformationPipeline(record, config);
                    
                    // Additional custom business logic
                    if (transformedRecord is Dictionary<string, object> dict)
                    {
                        // Validate employee data
                        if (dict.ContainsKey("Email") && dict["Email"] != null)
                        {
                            var email = dict["Email"].ToString();
                            if (!IsValidEmail(email))
                            {
                                dict["Email"] = null;
                                dict["ValidationNotes"] = "Invalid email format";
                            }
                        }

                        // Set department defaults
                        if (!dict.ContainsKey("Department") || dict["Department"] == null)
                        {
                            dict["Department"] = "Unassigned";
                        }
                    }

                    return transformedRecord;
                };

                // Use progress helper for detailed monitoring
                IProgress<IPassedArgs> progress = new Progress<IPassedArgs>(args =>
                {
                    // We don't need to report via helper here since helper expects IProgress<PassedArgs>
                    importManager.ProgressHelper.ReportProgress(null, args.Messege, args.ParameterInt1, args.ParameterInt2);
                    
                    // Custom progress logic
                    if (args.ParameterInt1 % 1000 == 0 && args.ParameterInt2 > 0) // Every 1000 records
                    {
                        var metrics = importManager.ProgressHelper.CalculatePerformanceMetrics(
                            DateTime.Now.AddMinutes(-10), args.ParameterInt1, args.ParameterInt2);
                        
                        Console.WriteLine($"Milestone: {args.ParameterInt1} records - " +
                                        $"Rate: {metrics.RecordsPerSecond:F1}/sec - " +
                                        $"ETA: {metrics.EstimatedTimeRemaining:hh\\:mm\\:ss}");
                    }
                });

                // Execute advanced import
                using var cts = new CancellationTokenSource();
                var result = await importManager.RunImportAsync(config, progress, cts.Token);

                // Advanced result analysis
                Console.WriteLine($"\nAdvanced Import Result: {result.Message}");

                // Try to use concrete helper methods if available
                var ph = importManager.ProgressHelper as DataImportProgressHelper;
                var errorLogs = ph != null
                    ? ph.GetLogEntriesByLevel(ImportLogLevel.Error)
                    : importManager.ImportLogData.Where(l => l.Level == ImportLogLevel.Error).ToList();
                var warningLogs = ph != null
                    ? ph.GetLogEntriesByLevel(ImportLogLevel.Warning)
                    : importManager.ImportLogData.Where(l => l.Level == ImportLogLevel.Warning).ToList();

                Console.WriteLine($"Errors found: {errorLogs.Count}");
                Console.WriteLine($"Warnings found: {warningLogs.Count}");

                // Export log for analysis
                var logText = ph != null
                    ? ph.ExportLogToText()
                    : string.Join(Environment.NewLine, importManager.ImportLogData.Select(e => $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{e.Level}] [{e.Category}] Record #{e.RecordNumber}: {e.Message}"));
                Console.WriteLine("Log exported for detailed analysis");

                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Example 5: Import with pause/resume and cancellation
        /// </summary>
        public async Task<bool> Example5_ControlledImport()
        {
            Console.WriteLine("\n=== Example 5: Controlled Import with Pause/Resume ===");

            using var importManager = new DataImportManager(_editor);

            try
            {
                var config = new DataImportConfiguration
                {
                    SourceEntityName = "LargeDataSet",
                    SourceDataSourceName = "BigDataSource",
                    DestEntityName = "ProcessedData",
                    DestDataSourceName = "TargetDatabase",
                    BatchSize = 500
                };

                // Start import in background
                using var cts = new CancellationTokenSource();
                IProgress<IPassedArgs> progress = new Progress<IPassedArgs>(args =>
                {
                    Console.WriteLine($"Background Progress: {args.Messege}");
                });

                // Start the import task
                var importTask = Task.Run(async () =>
                {
                    return await importManager.RunImportAsync(config, progress, cts.Token);
                });

                // Simulate user control
                await Task.Delay(2000); // Let it run for 2 seconds
                
                Console.WriteLine("Pausing import...");
                importManager.PauseImport();
                
                await Task.Delay(3000); // Pause for 3 seconds
                
                Console.WriteLine("Resuming import...");
                importManager.ResumeImport();
                
                await Task.Delay(2000); // Let it run for 2 more seconds
                
                // Check status
                var status = importManager.GetImportStatus();
                Console.WriteLine($"Import Status - Running: {status.IsRunning}, Paused: {status.IsPaused}");
                
                // Wait for completion or cancel if taking too long
                var completedTask = await Task.WhenAny(importTask, Task.Delay(10000)); // Wait max 10 seconds
                
                if (completedTask == importTask)
                {
                    var result = await importTask;
                    Console.WriteLine($"Controlled Import Result: {result.Message}");
                    return result.Flag == Errors.Ok;
                }
                else
                {
                    Console.WriteLine("Import taking too long, cancelling...");
                    importManager.CancelImport();
                    cts.Cancel();
                    
                    try
                    {
                        await importTask;
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Import successfully cancelled");
                    }
                    
                    return true; // Consider cancellation a successful test
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Integration Examples

        /// <summary>
        /// Example 6: Integration with DefaultsManager
        /// </summary>
        public async Task<bool> Example6_DefaultsManagerIntegration()
        {
            Console.WriteLine("\n=== Example 6: DefaultsManager Integration ===");

            using var importManager = new DataImportManager(_editor);

            try
            {
                var config = new DataImportConfiguration
                {
                    SourceEntityName = "CustomerImport",
                    SourceDataSourceName = "ImportSource",
                    DestEntityName = "Customers",
                    DestDataSourceName = "MainDB",
                    ApplyDefaults = true
                };

                // The DataImportManager automatically loads defaults from DefaultsManager
                // when ApplyDefaults is true and destination data source is set

                Console.WriteLine($"Default values loaded: {config.DefaultValues?.Count ?? 0}");

                // You can also add custom default values
                if (config.DefaultValues == null)
                    config.DefaultValues = new List<DefaultValue>();

                config.DefaultValues.Add(new DefaultValue
                {
                    PropertyName = "Status",
                    PropertyValue = "Active",
                    Rule = null // Static default
                });

                config.DefaultValues.Add(new DefaultValue
                {
                    PropertyName = "CreatedDate",
                    PropertyValue = null,
                    Rule = "NOW()" // Dynamic default using DefaultsManager resolver
                });

                // Custom transformation that works with defaults
                config.CustomTransformation = (record) =>
                {
                    // The transformation helper will apply defaults automatically
                    // Additional custom logic can be added here
                    
                    if (record is Dictionary<string, object> dict)
                    {
                        // Business-specific transformations after defaults are applied
                        if (dict.ContainsKey("CustomerType") && dict["CustomerType"] == null)
                        {
                            dict["CustomerType"] = "Standard"; // Business default
                        }
                    }
                    
                    return record;
                };

                IProgress<IPassedArgs> progress = new Progress<IPassedArgs>(args =>
                {
                    Console.WriteLine($"Defaults Integration Progress: {args.Messege}");
                });

                using var cts = new CancellationTokenSource();
                var result = await importManager.RunImportAsync(config, progress, cts.Token);

                Console.WriteLine($"DefaultsManager Integration Result: {result.Message}");
                Console.WriteLine($"Defaults applied automatically during transformation");

                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Simple email validation
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class for tracking import progress with detailed metrics
    /// </summary>
    public class ImportProgressTracker
    {
        private DateTime _startTime;
        private int _lastReportedCount;
        private DateTime _lastReportTime;

        public void ReportProgress(PassedArgs args)
        {
            if (_startTime == default)
            {
                _startTime = DateTime.Now;
                _lastReportTime = _startTime;
            }

            var now = DateTime.Now;
            var elapsed = now - _startTime;
            var recordsProcessed = args.ParameterInt1;
            var totalRecords = args.ParameterInt2;

            // Calculate rate
            var recordsSinceLastReport = recordsProcessed - _lastReportedCount;
            var timeSinceLastReport = now - _lastReportTime;
            var currentRate = timeSinceLastReport.TotalSeconds > 0 ? 
                recordsSinceLastReport / timeSinceLastReport.TotalSeconds : 0;

            // Update tracking
            _lastReportedCount = recordsProcessed;
            _lastReportTime = now;

            // Display progress
            if (totalRecords > 0)
            {
                var percentage = (recordsProcessed * 100.0) / totalRecords;
                var overallRate = elapsed.TotalSeconds > 0 ? recordsProcessed / elapsed.TotalSeconds : 0;
                var eta = overallRate > 0 ? 
                    TimeSpan.FromSeconds((totalRecords - recordsProcessed) / overallRate) : 
                    TimeSpan.Zero;

                Console.WriteLine($"[{now:HH:mm:ss}] {percentage:F1}% ({recordsProcessed}/{totalRecords}) " +
                                $"Rate: {overallRate:F1}/sec (Current: {currentRate:F1}/sec) " +
                                $"ETA: {eta:hh\\:mm\\:ss} - {args.Messege}");
            }
            else
            {
                Console.WriteLine($"[{now:HH:mm:ss}] {recordsProcessed} records - {args.Messege}");
            }
        }

        public void DisplaySummary()
        {
            var totalTime = DateTime.Now - _startTime;
            Console.WriteLine($"\nImport Summary:");
            Console.WriteLine($"Total Duration: {totalTime:hh\\:mm\\:ss}");
            Console.WriteLine($"Final Count: {_lastReportedCount} records");
            
            if (totalTime.TotalSeconds > 0)
            {
                var avgRate = _lastReportedCount / totalTime.TotalSeconds;
                Console.WriteLine($"Average Rate: {avgRate:F2} records/second");
            }
        }
    }
}
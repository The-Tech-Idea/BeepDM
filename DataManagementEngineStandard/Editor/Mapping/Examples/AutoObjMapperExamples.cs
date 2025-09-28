using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Mapping;
using TheTechIdea.Beep.Editor.Mapping.Extensions;

namespace TheTechIdea.Beep.Editor.Mapping.Examples
{
    /// <summary>
    /// Examples demonstrating the usage of the refactored AutoObjMapper
    /// </summary>
    public class AutoObjMapperExamples
    {
        // Sample classes for demonstration
        public class PersonDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
            public DateTime BirthDate { get; set; }
            public bool IsActive { get; set; }
        }

        public class Person
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
            public DateTime BirthDate { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime UpdatedDate { get; set; }
            public string FullName { get; set; }
        }

        /// <summary>
        /// Example 1: Basic mapping
        /// </summary>
        public static void BasicMappingExample()
        {
            // Create a mapper with default options
            var mapper = new AutoObjMapper();

            var dto = new PersonDto
            {
                FirstName = "John",
                LastName = "Doe",
                Age = 30,
                Email = "john.doe@example.com",
                BirthDate = new DateTime(1993, 5, 15),
                IsActive = true
            };

            // Map to new instance
            var person = mapper.Map<PersonDto, Person>(dto);

            // Map to existing instance
            var existingPerson = new Person();
            mapper.Map(dto, existingPerson);

            Console.WriteLine($"Mapped person: {person.FirstName} {person.LastName}");
        }

        /// <summary>
        /// Example 2: Factory methods for different scenarios
        /// </summary>
        public static void FactoryMethodsExample()
        {
            // High-performance mapper for production
            var fastMapper = AutoObjMapper.Factory.CreateHighPerformance();

            // Diagnostic mapper for debugging
            var debugMapper = AutoObjMapper.Factory.CreateDiagnostic();

            // Custom configured mapper
            var customMapper = AutoObjMapper.Factory.CreateWithConfiguration(options =>
                options.IgnoreNullSourceValues(true)
                       .EnableStatistics(true)
                       .UseCaseSensitivePropertyMatching(false));

            Console.WriteLine("Created mappers with different configurations");
        }

        /// <summary>
        /// Example 3: Advanced configuration with custom resolvers
        /// </summary>
        public static void AdvancedConfigurationExample()
        {
            var mapper = new AutoObjMapper();

            mapper.Configure(config =>
            {
                config.For<PersonDto, Person>()
                      .Ignore(nameof(Person.Id)) // Don't map Id field
                      .Ignore(nameof(Person.CreatedDate)) // Don't map CreatedDate
                      .ForMember(nameof(Person.FullName), dto => $"{dto.FirstName} {dto.LastName}")
                      .ForMember(nameof(Person.UpdatedDate), dto => DateTime.UtcNow)
                      .BeforeMap((dto, person) => 
                      {
                          Console.WriteLine($"Before mapping: {dto.FirstName}");
                      })
                      .AfterMap((dto, person) => 
                      {
                          person.CreatedDate = DateTime.UtcNow;
                          Console.WriteLine($"After mapping: {person.FullName}");
                      });
            });

            var dto = new PersonDto { FirstName = "Jane", LastName = "Smith", Age = 25 };
            var person = mapper.Map<PersonDto, Person>(dto);

            Console.WriteLine($"Custom mapped person: {person.FullName}");
        }

        /// <summary>
        /// Example 4: Collection mapping
        /// </summary>
        public static void CollectionMappingExample()
        {
            var mapper = new AutoObjMapper();

            var dtoList = new List<PersonDto>
            {
                new PersonDto { FirstName = "Alice", LastName = "Johnson", Age = 28 },
                new PersonDto { FirstName = "Bob", LastName = "Brown", Age = 35 },
                new PersonDto { FirstName = "Carol", LastName = "Davis", Age = 42 }
            };

            // Map collection using extension method
            var people = mapper.MapCollection<PersonDto, Person>(dtoList).ToList();

            // Map to existing collection
            var existingPeople = new List<Person>();
            mapper.MapCollection(dtoList, existingPeople, () => new Person());

            Console.WriteLine($"Mapped {people.Count} people from DTOs");
        }

        /// <summary>
        /// Example 5: Performance monitoring
        /// </summary>
        public static void PerformanceMonitoringExample()
        {
            var mapper = AutoObjMapper.Factory.CreateWithConfiguration(options =>
                options.EnableStatistics(true));

            var dto = new PersonDto { FirstName = "Performance", LastName = "Test", Age = 30 };

            // Map with monitoring
            var person = mapper.MapWithMonitoring<PersonDto, Person>(dto);

            // Get performance metrics
            var metrics = mapper.GetMappingMetrics<PersonDto, Person>();
            
            Console.WriteLine($"Mapping performed {metrics.TotalMappings} times");
            Console.WriteLine($"Average duration: {metrics.AverageDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Success rate: {metrics.SuccessRate:F1}%");

            // Get overall statistics
            var stats = mapper.GetStatistics();
            Console.WriteLine($"Total cached mappers: {stats.CachedMappersCount}");
        }

        /// <summary>
        /// Example 6: Validation and error handling
        /// </summary>
        public static void ValidationAndErrorHandlingExample()
        {
            var mapper = new AutoObjMapper();

            // Validate mapping configuration
            var validation = mapper.ValidateMapping<PersonDto, Person>();
            
            if (validation.IsValid)
            {
                Console.WriteLine("Mapping configuration is valid");
            }
            else
            {
                Console.WriteLine($"Validation errors: {string.Join(", ", validation.Errors)}");
            }

            // Safe mapping with result
            var dto = new PersonDto { FirstName = "Safe", LastName = "Mapping" };
            var result = mapper.TryMap<PersonDto, Person>(dto);

            if (result.IsSuccess)
            {
                Console.WriteLine($"Mapping successful: {result.Value.FirstName}");
            }
            else
            {
                Console.WriteLine($"Mapping failed: {result.Error}");
            }

            // Mapping with validation
            try
            {
                var person = mapper.MapWithValidation<PersonDto, Person>(dto);
                Console.WriteLine($"Validated mapping: {person.FirstName}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 7: Fluent configuration builder
        /// </summary>
        public static void FluentConfigurationExample()
        {
            var mapper = new AutoObjMapper();

            // Use fluent configuration
            var configuredMapper = mapper
                .CreateConfiguration<PersonDto, Person>()
                .Ignore(nameof(Person.Id))
                .ForMember(nameof(Person.FullName), dto => $"{dto.FirstName} {dto.LastName}")
                .BeforeMap((dto, person) => Console.WriteLine("Processing..."))
                .AfterMap((dto, person) => Console.WriteLine("Completed!"))
                .Build();

            var dto = new PersonDto { FirstName = "Fluent", LastName = "Config" };
            var person = configuredMapper.Map<PersonDto, Person>(dto);

            Console.WriteLine($"Fluent configured mapping: {person.FullName}");
        }

        /// <summary>
        /// Example 8: Detailed mapping with diagnostics
        /// </summary>
        public static void DetailedMappingExample()
        {
            var mapper = AutoObjMapper.Factory.CreateDiagnostic();

            var dto = new PersonDto { FirstName = "Detailed", LastName = "Mapping" };

            // Map with full details
            var detailedResult = mapper.MapWithDetails<PersonDto, Person>(dto, new Person());

            Console.WriteLine($"Mapping result: {detailedResult}");
            Console.WriteLine($"Validation warnings: {detailedResult.ValidationResult?.Warnings?.Count ?? 0}");
            Console.WriteLine($"Elapsed time: {detailedResult.ElapsedTime.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Example 9: Performance tracking
        /// </summary>
        public static void PerformanceTrackingExample()
        {
            var mapper = new AutoObjMapper();
            var dto = new PersonDto { FirstName = "Performance", LastName = "Tracking" };

            // Map with performance tracking
            var person = mapper.MapWithPerformanceTracking(dto, new Person(), out TimeSpan elapsed);

            Console.WriteLine($"Mapping completed in {elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Mapped person: {person.FirstName} {person.LastName}");
        }

        /// <summary>
        /// Example 10: Using utilities for quick operations
        /// </summary>
        public static void UtilitiesExample()
        {
            var dto = new PersonDto { FirstName = "Utility", LastName = "Test" };

            // Using static utility methods
            var person1 = AutoObjMapperUtilities.Map<PersonDto, Person>(dto);
            Console.WriteLine($"Utility mapping 1: {person1.FirstName} {person1.LastName}");

            // Safe mapping with utilities
            var result = AutoObjMapperUtilities.TryMap<PersonDto, Person>(dto);
            if (result.IsSuccess)
            {
                Console.WriteLine($"Safe utility mapping: {result.Value.FirstName} {result.Value.LastName}");
            }

            // Validation with utilities
            var validation = AutoObjMapperUtilities.ValidateMapping<PersonDto, Person>();
            Console.WriteLine($"Validation result: {validation.IsValid}");

            // Get statistics
            var stats = AutoObjMapperUtilities.GetStatistics();
            Console.WriteLine($"Cached mappers: {stats.CachedMappersCount}");

            // Create specialized mappers
            var fastMapper = AutoObjMapperUtilities.CreateFastMapper();
            var diagnosticMapper = AutoObjMapperUtilities.CreateDiagnosticMapper();

            Console.WriteLine("Created specialized mappers using utilities");
        }

        /// <summary>
        /// Run all examples
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("=== AutoObjMapper Examples ===\n");

            Console.WriteLine("1. Basic Mapping:");
            BasicMappingExample();
            Console.WriteLine();

            Console.WriteLine("2. Factory Methods:");
            FactoryMethodsExample();
            Console.WriteLine();

            Console.WriteLine("3. Advanced Configuration:");
            AdvancedConfigurationExample();
            Console.WriteLine();

            Console.WriteLine("4. Collection Mapping:");
            CollectionMappingExample();
            Console.WriteLine();

            Console.WriteLine("5. Performance Monitoring:");
            PerformanceMonitoringExample();
            Console.WriteLine();

            Console.WriteLine("6. Validation and Error Handling:");
            ValidationAndErrorHandlingExample();
            Console.WriteLine();

            Console.WriteLine("7. Fluent Configuration:");
            FluentConfigurationExample();
            Console.WriteLine();

            Console.WriteLine("8. Detailed Mapping:");
            DetailedMappingExample();
            Console.WriteLine();

            Console.WriteLine("9. Performance Tracking:");
            PerformanceTrackingExample();
            Console.WriteLine();

            Console.WriteLine("10. Utilities Example:");
            UtilitiesExample();
            Console.WriteLine();

            Console.WriteLine("=== All Examples Completed ===");
        }
    }
}
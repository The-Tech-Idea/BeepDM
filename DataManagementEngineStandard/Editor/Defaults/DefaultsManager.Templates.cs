using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Defaults
{
    /// <summary>
    /// Template creation methods for DefaultsManager
    /// </summary>
    public partial class DefaultsManager
    {
        #region Template Creation Methods

        /// <summary>
        /// Creates audit template with common audit fields
        /// </summary>
        private static List<DefaultValue> CreateAuditTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "CreatedDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Timestamp when the record was created"
                },
                new DefaultValue
                {
                    PropertyName = "CreatedBy",
                    propertyType = DefaultValueType.CurrentUser,
                    Rule = "CurrentUser",
                    Description = "User who created the record"
                },
                new DefaultValue
                {
                    PropertyName = "ModifiedDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Timestamp when the record was last modified"
                },
                new DefaultValue
                {
                    PropertyName = "ModifiedBy",
                    propertyType = DefaultValueType.CurrentUser,
                    Rule = "CurrentUser",
                    Description = "User who last modified the record"
                },
                new DefaultValue
                {
                    PropertyName = "IsActive",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = true,
                    Description = "Whether the record is active"
                },
                new DefaultValue
                {
                    PropertyName = "Version",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = 1,
                    Description = "Version number for optimistic concurrency"
                },
                new DefaultValue
                {
                    PropertyName = "RowGuid",
                    propertyType = DefaultValueType.GenerateUniqueId,
                    Rule = "GenerateUniqueId",
                    Description = "Unique identifier for the record"
                }
            };
        }

        /// <summary>
        /// Creates user management template
        /// </summary>
        private static List<DefaultValue> CreateUserManagementTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "IsActive",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = true,
                    Description = "Whether the user account is active"
                },
                new DefaultValue
                {
                    PropertyName = "CreatedDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "When the user account was created"
                },
                new DefaultValue
                {
                    PropertyName = "LastLoginDate",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = null,
                    Description = "Last login timestamp"
                },
                new DefaultValue
                {
                    PropertyName = "LoginAttempts",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = 0,
                    Description = "Number of failed login attempts"
                },
                new DefaultValue
                {
                    PropertyName = "IsLocked",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = false,
                    Description = "Whether the account is locked"
                },
                new DefaultValue
                {
                    PropertyName = "Role",
                    propertyType = DefaultValueType.RoleBasedValue,
                    Rule = "RoleBasedValue:DefaultRole",
                    Description = "Default user role"
                },
                new DefaultValue
                {
                    PropertyName = "PasswordExpiry",
                    propertyType = DefaultValueType.BusinessCalendar,
                    Rule = "BusinessCalendar:AddDays:90",
                    Description = "Password expiration date"
                }
            };
        }

        /// <summary>
        /// Creates order processing template
        /// </summary>
        private static List<DefaultValue> CreateOrderProcessingTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "OrderDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Date when the order was placed"
                },
                new DefaultValue
                {
                    PropertyName = "Status",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = "Pending",
                    Description = "Initial order status"
                },
                new DefaultValue
                {
                    PropertyName = "Priority",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = "Normal",
                    Description = "Order processing priority"
                },
                new DefaultValue
                {
                    PropertyName = "Total",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = 0.00m,
                    Description = "Order total amount"
                },
                new DefaultValue
                {
                    PropertyName = "CreatedBy",
                    propertyType = DefaultValueType.CurrentUser,
                    Rule = "CurrentUser",
                    Description = "User who created the order"
                },
                new DefaultValue
                {
                    PropertyName = "OrderNumber",
                    propertyType = DefaultValueType.Sequence,
                    Rule = "Sequence:OrderNumber",
                    Description = "Auto-generated order number"
                },
                new DefaultValue
                {
                    PropertyName = "ExpectedDelivery",
                    propertyType = DefaultValueType.BusinessCalendar,
                    Rule = "BusinessCalendar:AddBusinessDays:5",
                    Description = "Expected delivery date"
                }
            };
        }

        /// <summary>
        /// Creates customer management template
        /// </summary>
        private static List<DefaultValue> CreateCustomerManagementTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "CustomerType",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = "Standard",
                    Description = "Default customer type"
                },
                new DefaultValue
                {
                    PropertyName = "Status",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = "Active",
                    Description = "Customer account status"
                },
                new DefaultValue
                {
                    PropertyName = "RegistrationDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Customer registration date"
                },
                new DefaultValue
                {
                    PropertyName = "CreditLimit",
                    propertyType = DefaultValueType.ConfigurationValue,
                    Rule = "ConfigurationValue:DefaultCreditLimit",
                    Description = "Initial credit limit"
                },
                new DefaultValue
                {
                    PropertyName = "PreferredLanguage",
                    propertyType = DefaultValueType.LocalizedValue,
                    Rule = "LocalizedValue:DefaultLanguage",
                    Description = "Customer's preferred language"
                },
                new DefaultValue
                {
                    PropertyName = "CustomerCode",
                    propertyType = DefaultValueType.Sequence,
                    Rule = "Sequence:CustomerCode",
                    Description = "Auto-generated customer code"
                },
                new DefaultValue
                {
                    PropertyName = "Region",
                    propertyType = DefaultValueType.LocationBased,
                    Rule = "LocationBased:DefaultRegion",
                    Description = "Customer's region based on location"
                }
            };
        }

        /// <summary>
        /// Creates product catalog template
        /// </summary>
        private static List<DefaultValue> CreateProductCatalogTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "IsActive",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = true,
                    Description = "Whether the product is active"
                },
                new DefaultValue
                {
                    PropertyName = "Category",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = "General",
                    Description = "Default product category"
                },
                new DefaultValue
                {
                    PropertyName = "CreatedDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Product creation date"
                },
                new DefaultValue
                {
                    PropertyName = "StockQuantity",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = 0,
                    Description = "Initial stock quantity"
                },
                new DefaultValue
                {
                    PropertyName = "Price",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = 0.00m,
                    Description = "Product price"
                },
                new DefaultValue
                {
                    PropertyName = "SKU",
                    propertyType = DefaultValueType.Sequence,
                    Rule = "Sequence:ProductSKU",
                    Description = "Auto-generated SKU"
                },
                new DefaultValue
                {
                    PropertyName = "ReorderLevel",
                    propertyType = DefaultValueType.ConfigurationValue,
                    Rule = "ConfigurationValue:DefaultReorderLevel",
                    Description = "Minimum stock level before reorder"
                }
            };
        }

        /// <summary>
        /// Creates financial template
        /// </summary>
        private static List<DefaultValue> CreateFinancialTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "TransactionDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Financial transaction date"
                },
                new DefaultValue
                {
                    PropertyName = "Amount",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = 0.00m,
                    Description = "Transaction amount"
                },
                new DefaultValue
                {
                    PropertyName = "Currency",
                    propertyType = DefaultValueType.ConfigurationValue,
                    Rule = "ConfigurationValue:DefaultCurrency",
                    Description = "Transaction currency"
                },
                new DefaultValue
                {
                    PropertyName = "Status",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = "Pending",
                    Description = "Transaction status"
                },
                new DefaultValue
                {
                    PropertyName = "CreatedBy",
                    propertyType = DefaultValueType.CurrentUser,
                    Rule = "CurrentUser",
                    Description = "User who created the transaction"
                },
                new DefaultValue
                {
                    PropertyName = "TransactionNumber",
                    propertyType = DefaultValueType.Sequence,
                    Rule = "Sequence:TransactionNumber",
                    Description = "Auto-generated transaction number"
                },
                new DefaultValue
                {
                    PropertyName = "ExchangeRate",
                    propertyType = DefaultValueType.WebService,
                    Rule = "WebService:GetExchangeRate",
                    Description = "Current exchange rate"
                }
            };
        }

        /// <summary>
        /// Creates inventory template
        /// </summary>
        private static List<DefaultValue> CreateInventoryTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "Quantity",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = 0,
                    Description = "Current inventory quantity"
                },
                new DefaultValue
                {
                    PropertyName = "MinimumStock",
                    propertyType = DefaultValueType.ConfigurationValue,
                    Rule = "ConfigurationValue:DefaultMinimumStock",
                    Description = "Minimum stock level"
                },
                new DefaultValue
                {
                    PropertyName = "LastUpdated",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Last inventory update time"
                },
                new DefaultValue
                {
                    PropertyName = "Location",
                    propertyType = DefaultValueType.LocationBased,
                    Rule = "LocationBased:DefaultWarehouse",
                    Description = "Storage location"
                },
                new DefaultValue
                {
                    PropertyName = "UpdatedBy",
                    propertyType = DefaultValueType.CurrentUser,
                    Rule = "CurrentUser",
                    Description = "User who updated the inventory"
                },
                new DefaultValue
                {
                    PropertyName = "CostPerUnit",
                    propertyType = DefaultValueType.Statistical,
                    Rule = "Statistical:AverageCost",
                    Description = "Average cost per unit"
                },
                new DefaultValue
                {
                    PropertyName = "ReorderPoint",
                    propertyType = DefaultValueType.MLPrediction,
                    Rule = "MLPrediction:OptimalReorderPoint",
                    Description = "AI-predicted optimal reorder point"
                }
            };
        }

        /// <summary>
        /// Creates basic template with minimal fields
        /// </summary>
        private static List<DefaultValue> CreateBasicTemplate()
        {
            return new List<DefaultValue>
            {
                new DefaultValue
                {
                    PropertyName = "Id",
                    propertyType = DefaultValueType.GenerateUniqueId,
                    Rule = "GenerateUniqueId",
                    Description = "Unique identifier"
                },
                new DefaultValue
                {
                    PropertyName = "IsActive",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = true,
                    Description = "Whether the record is active"
                },
                new DefaultValue
                {
                    PropertyName = "CreatedDate",
                    propertyType = DefaultValueType.CurrentDateTime,
                    Rule = "CurrentDateTime",
                    Description = "Record creation date"
                },
                new DefaultValue
                {
                    PropertyName = "Name",
                    propertyType = DefaultValueType.Static,
                    PropertyValue = "New Record",
                    Description = "Default name for new records"
                }
            };
        }

        #endregion

        #region Advanced Template Methods

        /// <summary>
        /// Creates a custom template based on entity structure analysis
        /// </summary>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="includeAudit">Whether to include audit fields</param>
        /// <param name="includeWorkflow">Whether to include workflow fields</param>
        /// <returns>List of intelligent default values</returns>
        public static List<DefaultValue> CreateIntelligentTemplate(string entityName, bool includeAudit = true, bool includeWorkflow = false)
        {
            var template = new List<DefaultValue>();

            // Add basic ID field
            template.Add(new DefaultValue
            {
                PropertyName = "ID",
                propertyType = DefaultValueType.AutoIncrement,
                Rule = "AutoIncrement",
                Description = "Auto-incrementing primary key"
            });

            // Add audit fields if requested
            if (includeAudit)
            {
                template.AddRange(CreateAuditTemplate());
            }

            // Add workflow fields if requested
            if (includeWorkflow)
            {
                template.AddRange(new List<DefaultValue>
                {
                    new DefaultValue
                    {
                        PropertyName = "WorkflowStatus",
                        propertyType = DefaultValueType.WorkflowContext,
                        Rule = "WorkflowContext:InitialStatus",
                        Description = "Initial workflow status"
                    },
                    new DefaultValue
                    {
                        PropertyName = "AssignedTo",
                        propertyType = DefaultValueType.WorkflowContext,
                        Rule = "WorkflowContext:DefaultAssignee",
                        Description = "Default task assignee"
                    }
                });
            }

            // Add entity-specific intelligent defaults based on name patterns
            if (entityName.ToLowerInvariant().Contains("user"))
            {
                template.AddRange(CreateUserManagementTemplate());
            }
            else if (entityName.ToLowerInvariant().Contains("order"))
            {
                template.AddRange(CreateOrderProcessingTemplate());
            }
            else if (entityName.ToLowerInvariant().Contains("customer"))
            {
                template.AddRange(CreateCustomerManagementTemplate());
            }
            else if (entityName.ToLowerInvariant().Contains("product") || entityName.ToLowerInvariant().Contains("inventory"))
            {
                template.AddRange(CreateInventoryTemplate());
            }

            return template;
        }

        /// <summary>
        /// Gets all available template names
        /// </summary>
        /// <returns>List of available template names</returns>
        public static List<string> GetAvailableTemplateNames()
        {
            return new List<string>
            {
                "audit",
                "usermanagement", 
                "orderprocessing",
                "customermanagement",
                "productcatalog",
                "financial",
                "inventory",
                "basic"
            };
        }

        /// <summary>
        /// Gets template description for a given template name
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <returns>Description of what the template provides</returns>
        public static string GetTemplateDescription(string templateName)
        {
            return templateName.ToLowerInvariant() switch
            {
                "audit" => "Common audit fields: CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, IsActive, Version, RowGuid",
                "usermanagement" => "User account fields: IsActive, CreatedDate, LastLoginDate, LoginAttempts, IsLocked, Role, PasswordExpiry",
                "orderprocessing" => "Order processing fields: OrderDate, Status, Priority, Total, CreatedBy, OrderNumber, ExpectedDelivery",
                "customermanagement" => "Customer fields: CustomerType, Status, RegistrationDate, CreditLimit, PreferredLanguage, CustomerCode, Region",
                "productcatalog" => "Product fields: IsActive, Category, CreatedDate, StockQuantity, Price, SKU, ReorderLevel",
                "financial" => "Financial transaction fields: TransactionDate, Amount, Currency, Status, CreatedBy, TransactionNumber, ExchangeRate",
                "inventory" => "Inventory fields: Quantity, MinimumStock, LastUpdated, Location, UpdatedBy, CostPerUnit, ReorderPoint",
                "basic" => "Basic fields: Id, IsActive, CreatedDate, Name",
                _ => "Unknown template"
            };
        }

        #endregion
    }
}
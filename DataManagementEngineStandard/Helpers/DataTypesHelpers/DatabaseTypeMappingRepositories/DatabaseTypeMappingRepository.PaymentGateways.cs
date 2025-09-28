using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Payment Gateway and Financial Services specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of Stripe data type mappings.</summary>
        /// <returns>A list of Stripe data type mappings.</returns>
        public static List<DatatypeMapping> GetStripeDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Stripe API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "StripeDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "StripeDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "StripeDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "StripeDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "StripeDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "StripeDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "hash", DataSourceName = "StripeDataSource", NetDataType = "System.Collections.Generic.Dictionary<string, object>", Fav = false }
            };
        }

        /// <summary>Returns a list of PayPal data type mappings.</summary>
        /// <returns>A list of PayPal data type mappings.</returns>
        public static List<DatatypeMapping> GetPayPalDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // PayPal API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "PayPalDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "PayPalDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "PayPalDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date_time", DataSourceName = "PayPalDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "PayPalDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "PayPalDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Square data type mappings.</summary>
        /// <returns>A list of Square data type mappings.</returns>
        public static List<DatatypeMapping> GetSquareDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Square API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "SquareDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "SquareDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "SquareDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "rfc3339_timestamp", DataSourceName = "SquareDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "money", DataSourceName = "SquareDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "SquareDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Authorize.Net data type mappings.</summary>
        /// <returns>A list of Authorize.Net data type mappings.</returns>
        public static List<DatatypeMapping> GetAuthorizeNetDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Authorize.Net API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "AuthorizeNetDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "AuthorizeNetDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "AuthorizeNetDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "dateTime", DataSourceName = "AuthorizeNetDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "int", DataSourceName = "AuthorizeNetDataSource", NetDataType = "System.Int32", Fav = false }
            };
        }

        /// <summary>Returns a list of Braintree data type mappings.</summary>
        /// <returns>A list of Braintree data type mappings.</returns>
        public static List<DatatypeMapping> GetBraintreeDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Braintree API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "BraintreeDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "BraintreeDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "BraintreeDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "BraintreeDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "BraintreeDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "BraintreeDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Worldpay data type mappings.</summary>
        /// <returns>A list of Worldpay data type mappings.</returns>
        public static List<DatatypeMapping> GetWorldpayDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Worldpay API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "WorldpayDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "WorldpayDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "WorldpayDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "WorldpayDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "dateTime", DataSourceName = "WorldpayDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "WorldpayDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Adyen data type mappings.</summary>
        /// <returns>A list of Adyen data type mappings.</returns>
        public static List<DatatypeMapping> GetAdyenDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Adyen API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "AdyenDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "AdyenDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "AdyenDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date-time", DataSourceName = "AdyenDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "AdyenDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "AdyenDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of TwoCheckout data type mappings.</summary>
        /// <returns>A list of TwoCheckout data type mappings.</returns>
        public static List<DatatypeMapping> GetTwoCheckoutDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // TwoCheckout API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TwoCheckoutDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "TwoCheckoutDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TwoCheckoutDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "TwoCheckoutDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "TwoCheckoutDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Razorpay data type mappings.</summary>
        /// <returns>A list of Razorpay data type mappings.</returns>
        public static List<DatatypeMapping> GetRazorpayDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Razorpay API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "RazorpayDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "RazorpayDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "RazorpayDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "RazorpayDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "json", DataSourceName = "RazorpayDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "RazorpayDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Payoneer data type mappings.</summary>
        /// <returns>A list of Payoneer data type mappings.</returns>
        public static List<DatatypeMapping> GetPayoneerDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Payoneer API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "PayoneerDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "decimal", DataSourceName = "PayoneerDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "PayoneerDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "dateTime", DataSourceName = "PayoneerDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "PayoneerDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Wise data type mappings.</summary>
        /// <returns>A list of Wise data type mappings.</returns>
        public static List<DatatypeMapping> GetWiseDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Wise API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "WiseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "WiseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "WiseDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "date-time", DataSourceName = "WiseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "WiseDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "WiseDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Coinbase data type mappings.</summary>
        /// <returns>A list of Coinbase data type mappings.</returns>
        public static List<DatatypeMapping> GetCoinbaseDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Coinbase API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "CoinbaseDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "CoinbaseDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "CoinbaseDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "CoinbaseDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "CoinbaseDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "CoinbaseDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Venmo data type mappings.</summary>
        /// <returns>A list of Venmo data type mappings.</returns>
        public static List<DatatypeMapping> GetVenmoDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Venmo API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "VenmoDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "VenmoDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "VenmoDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "VenmoDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "VenmoDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of BitPay data type mappings.</summary>
        /// <returns>A list of BitPay data type mappings.</returns>
        public static List<DatatypeMapping> GetBitPayDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // BitPay API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "BitPayDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "number", DataSourceName = "BitPayDataSource", NetDataType = "System.Decimal", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "BitPayDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "dateTime", DataSourceName = "BitPayDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "BitPayDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "BitPayDataSource", NetDataType = "System.Array", Fav = false }
            };
        }
    }
}
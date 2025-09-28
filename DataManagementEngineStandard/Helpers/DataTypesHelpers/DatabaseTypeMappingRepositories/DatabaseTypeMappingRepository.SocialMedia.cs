using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Repository containing Social Media platform specific type mappings.
    /// </summary>
    public static partial class DatabaseTypeMappingRepository
    {
        /// <summary>Returns a list of Facebook data type mappings.</summary>
        /// <returns>A list of Facebook data type mappings.</returns>
        public static List<DatatypeMapping> GetFacebookDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Facebook Graph API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "FacebookDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "numeric string", DataSourceName = "FacebookDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "FacebookDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "FacebookDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "FacebookDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "FacebookDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "FacebookDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "enum", DataSourceName = "FacebookDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "list", DataSourceName = "FacebookDataSource", NetDataType = "System.Collections.Generic.List<object>", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "FacebookDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Twitter data type mappings.</summary>
        /// <returns>A list of Twitter data type mappings.</returns>
        public static List<DatatypeMapping> GetTwitterDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Twitter API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TwitterDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "TwitterDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TwitterDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "TwitterDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "TwitterDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "TwitterDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "url", DataSourceName = "TwitterDataSource", NetDataType = "System.String", Fav = false }
            };
        }

        /// <summary>Returns a list of Instagram data type mappings.</summary>
        /// <returns>A list of Instagram data type mappings.</returns>
        public static List<DatatypeMapping> GetInstagramDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Instagram API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "InstagramDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "InstagramDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "InstagramDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "InstagramDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "media_url", DataSourceName = "InstagramDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "media_type", DataSourceName = "InstagramDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "InstagramDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of LinkedIn data type mappings.</summary>
        /// <returns>A list of LinkedIn data type mappings.</returns>
        public static List<DatatypeMapping> GetLinkedInDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // LinkedIn API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "LinkedInDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "LinkedInDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "LinkedInDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "LinkedInDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "urn", DataSourceName = "LinkedInDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "localized_string", DataSourceName = "LinkedInDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "LinkedInDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Pinterest data type mappings.</summary>
        /// <returns>A list of Pinterest data type mappings.</returns>
        public static List<DatatypeMapping> GetPinterestDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Pinterest API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "PinterestDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "PinterestDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "PinterestDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "PinterestDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "image_url", DataSourceName = "PinterestDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "color", DataSourceName = "PinterestDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "PinterestDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of YouTube data type mappings.</summary>
        /// <returns>A list of YouTube data type mappings.</returns>
        public static List<DatatypeMapping> GetYouTubeDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // YouTube API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "YouTubeDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "unsigned long", DataSourceName = "YouTubeDataSource", NetDataType = "System.UInt64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "YouTubeDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "duration", DataSourceName = "YouTubeDataSource", NetDataType = "System.TimeSpan", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "YouTubeDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "YouTubeDataSource", NetDataType = "System.Object", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "YouTubeDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of TikTok data type mappings.</summary>
        /// <returns>A list of TikTok data type mappings.</returns>
        public static List<DatatypeMapping> GetTikTokDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // TikTok API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TikTokDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "TikTokDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TikTokDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "TikTokDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "video_url", DataSourceName = "TikTokDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "TikTokDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Snapchat data type mappings.</summary>
        /// <returns>A list of Snapchat data type mappings.</returns>
        public static List<DatatypeMapping> GetSnapchatDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Snapchat API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "SnapchatDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "SnapchatDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "SnapchatDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "SnapchatDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "media_url", DataSourceName = "SnapchatDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "SnapchatDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Reddit data type mappings.</summary>
        /// <returns>A list of Reddit data type mappings.</returns>
        public static List<DatatypeMapping> GetRedditDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Reddit API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "RedditDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "RedditDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "RedditDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "float", DataSourceName = "RedditDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "RedditDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "RedditDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "RedditDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Buffer data type mappings.</summary>
        /// <returns>A list of Buffer data type mappings.</returns>
        public static List<DatatypeMapping> GetBufferDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Buffer API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "BufferDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "BufferDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "BufferDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "BufferDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "BufferDataSource", NetDataType = "System.Array", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "object", DataSourceName = "BufferDataSource", NetDataType = "System.Object", Fav = false }
            };
        }

        /// <summary>Returns a list of Hootsuite data type mappings.</summary>
        /// <returns>A list of Hootsuite data type mappings.</returns>
        public static List<DatatypeMapping> GetHootsuiteDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Hootsuite API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "HootsuiteDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "HootsuiteDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "HootsuiteDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "HootsuiteDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "url", DataSourceName = "HootsuiteDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "HootsuiteDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of TikTokAds data type mappings.</summary>
        /// <returns>A list of TikTokAds data type mappings.</returns>
        public static List<DatatypeMapping> GetTikTokAdsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // TikTok Ads API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "TikTokAdsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "TikTokAdsDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "double", DataSourceName = "TikTokAdsDataSource", NetDataType = "System.Double", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "TikTokAdsDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "TikTokAdsDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "TikTokAdsDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Threads data type mappings.</summary>
        /// <returns>A list of Threads data type mappings.</returns>
        public static List<DatatypeMapping> GetThreadsDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Threads API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "ThreadsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "ThreadsDataSource", NetDataType = "System.Int64", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "ThreadsDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "timestamp", DataSourceName = "ThreadsDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "media_url", DataSourceName = "ThreadsDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "ThreadsDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Mastodon data type mappings.</summary>
        /// <returns>A list of Mastodon data type mappings.</returns>
        public static List<DatatypeMapping> GetMastodonDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Mastodon API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "MastodonDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "MastodonDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "MastodonDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "MastodonDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "url", DataSourceName = "MastodonDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "MastodonDataSource", NetDataType = "System.Array", Fav = false }
            };
        }

        /// <summary>Returns a list of Bluesky data type mappings.</summary>
        /// <returns>A list of Bluesky data type mappings.</returns>
        public static List<DatatypeMapping> GetBlueskyDataTypeMappings()
        {
            return new List<DatatypeMapping>
            {
                // Bluesky API data types
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "string", DataSourceName = "BlueskyDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "integer", DataSourceName = "BlueskyDataSource", NetDataType = "System.Int32", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "boolean", DataSourceName = "BlueskyDataSource", NetDataType = "System.Boolean", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "datetime", DataSourceName = "BlueskyDataSource", NetDataType = "System.DateTime", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "did", DataSourceName = "BlueskyDataSource", NetDataType = "System.String", Fav = false },
                new DatatypeMapping { ID = 0, GuidID = Guid.NewGuid().ToString(), DataType = "array", DataSourceName = "BlueskyDataSource", NetDataType = "System.Array", Fav = false }
            };
        }
    }
}
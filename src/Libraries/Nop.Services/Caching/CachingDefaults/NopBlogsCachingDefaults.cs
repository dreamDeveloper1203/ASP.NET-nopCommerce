﻿using Nop.Core.Caching;

namespace Nop.Services.Caching.CachingDefaults
{
    /// <summary>
    /// Represents default values related to blogs services
    /// </summary>
    public static partial class NopBlogsCachingDefaults
    {
        /// <summary>
        /// Key for number of blog comments
        /// </summary>
        /// <remarks>
        /// {0} : blog post ID
        /// {1} : store ID
        /// {2} : are only approved comments?
        /// </remarks>
        public static CacheKey BlogCommentsNumberCacheKey => new CacheKey("Nop.blog.comments.number-{0}-{1}-{2}", BlogCommentsNumberPrefixCacheKey);

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        /// <remarks>
        /// {0} : blog post ID
        /// </remarks>
        public static string BlogCommentsNumberPrefixCacheKey => "Nop.blog.comments.number-{0}";

        /// <summary>
        /// Key for blog tag list model
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : current store ID
        /// </remarks>
        public static CacheKey BlogTagsModelCacheKey => new CacheKey("Nop.blog.tags-{0}-{1}", BlogTagsPrefixCacheKey);

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string BlogTagsPrefixCacheKey => "Nop.blog.tags";
    }
}
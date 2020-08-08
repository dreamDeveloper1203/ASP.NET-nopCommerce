﻿using Nop.Core.Caching;
using Nop.Core.Domain.Shipping;
using Nop.Services.Caching;

namespace Nop.Services.Shipping.Caching
{
    /// <summary>
    /// Represents a product availability range cache event consumer
    /// </summary>
    public partial class ProductAvailabilityRangeCacheEventConsumer : CacheEventConsumer<ProductAvailabilityRange>
    {
        /// <summary>
        /// Clear cache data
        /// </summary>
        /// <param name="entity">Entity</param>
        protected override void ClearCache(ProductAvailabilityRange entity)
        {
            Remove(_staticCacheManager.PrepareKey(NopCachingDefaults.AllEntitiesCacheKey, entity.GetType().Name.ToLower()));
        }
    }
}
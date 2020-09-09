﻿using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Caching;

namespace Nop.Services.Catalog.Caching
{
    /// <summary>
    /// Represents a product manufacturer cache event consumer
    /// </summary>
    public partial class ProductManufacturerCacheEventConsumer : CacheEventConsumer<ProductManufacturer>
    {
        /// <summary>
        /// Clear cache data
        /// </summary>
        /// <param name="entity">Entity</param>
        protected override async Task ClearCache(ProductManufacturer entity)
        {
            var prefix = _cacheKeyService.PrepareKeyPrefix(NopCatalogDefaults.ProductManufacturersByProductPrefixCacheKey, entity.ProductId);
            await RemoveByPrefix(prefix);
            
            prefix = _cacheKeyService.PrepareKeyPrefix(NopCatalogDefaults.ProductPricePrefixCacheKey, entity.ProductId);
            await RemoveByPrefix(prefix);
        }
    }
}

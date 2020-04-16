﻿using Nop.Core.Domain.Catalog;
using Nop.Services.Caching;
using Nop.Services.Orders;

namespace Nop.Services.Catalog.Caching
{
    /// <summary>
    /// Represents a product cache event consumer
    /// </summary>
    public partial class ProductCacheEventConsumer : CacheEventConsumer<Product>
    {
        /// <summary>
        /// Clear cache data
        /// </summary>
        /// <param name="entity">Entity</param>
        protected override void ClearCache(Product entity)
        {
            var prefix = NopCatalogDefaults.ProductManufacturersByProductPrefixCacheKey.ToCacheKey(entity);
            RemoveByPrefix(prefix);

            Remove(NopCatalogDefaults.ProductsAllDisplayedOnHomepageCacheKey);
            RemoveByPrefix(NopCatalogDefaults.ProductsByIdsPrefixCacheKey);

            prefix = NopCatalogDefaults.ProductPricePrefixCacheKey.ToCacheKey(entity);
            RemoveByPrefix(prefix);

            RemoveByPrefix(NopOrderDefaults.ShoppingCartPrefixCacheKey);
        }
    }
}

﻿using Nop.Core.Domain.Catalog;
using Nop.Services.Caching.CachingDefaults;

namespace Nop.Services.Caching.CacheEventConsumers.Catalog
{
    public partial class ProductCacheEventConsumer : CacheEventConsumer<Product>
    {
        protected override void ClearCache(Product entity)
        {
            RemoveByPrefix(NopCatalogCachingDefaults.ProductsPrefixCacheKey);
            RemoveByPrefix(NopCatalogCachingDefaults.ProductPricePrefixCacheKey);
            RemoveByPrefix(NopNewsCachingDefaults.ShoppingCartPrefixCacheKey);
        }
    }
}

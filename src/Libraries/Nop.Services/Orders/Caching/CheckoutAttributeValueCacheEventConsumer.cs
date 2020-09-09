﻿using Nop.Core.Domain.Orders;
using Nop.Services.Caching;
using System.Threading.Tasks;

namespace Nop.Services.Orders.Caching
{
    /// <summary>
    /// Represents a checkout attribute value cache event consumer
    /// </summary>
    public partial class CheckoutAttributeValueCacheEventConsumer : CacheEventConsumer<CheckoutAttributeValue>
    {
        /// <summary>
        /// Clear cache data
        /// </summary>
        /// <param name="entity">Entity</param>
        protected override async Task ClearCache(CheckoutAttributeValue entity)
        {
            var cacheKey = _cacheKeyService.PrepareKey(NopOrderDefaults.CheckoutAttributeValuesAllCacheKey, entity.CheckoutAttributeId);
            await Remove(cacheKey);
        }
    }
}

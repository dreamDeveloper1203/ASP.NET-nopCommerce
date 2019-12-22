﻿using Nop.Core.Domain.Media;
using Nop.Services.Caching.CachingDefaults;

namespace Nop.Services.Caching.CacheEventConsumers.Media
{
    public partial class PictureCacheEventConsumer : CacheEventConsumer<Picture>
    {
        public override void ClearCashe(Picture entity)
        {
            _cacheManager.RemoveByPrefix(NopMediaCachingDefaults.ThumbsPrefixCacheKey);
        }
    }
}

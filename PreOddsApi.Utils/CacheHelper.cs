using System;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace PreOddsApi.Utils
{
    public class CacheHelper : ICacheHelper
    {
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
        private readonly IMemoryCache _memoryCache;

        public CacheHelper(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void Set(string key, object value, int timeInSeconds)
        {
            if (timeInSeconds <= 0)
            {
                return;
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(timeInSeconds));
            cacheEntryOptions.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
            _memoryCache.Set(key, value, cacheEntryOptions);
        }

        public object Get(string key)
        {
            if (_memoryCache.TryGetValue(key, out object value))
            {
                return value;
            }
            return null;
        }

        public void ClearCache()
        {
            if (_resetCacheToken != null && !_resetCacheToken.IsCancellationRequested && _resetCacheToken.Token.CanBeCanceled)
            {
                _resetCacheToken.Cancel();
                _resetCacheToken.Dispose();
            }

            _resetCacheToken = new CancellationTokenSource();
        }
    }

    public interface ICacheHelper
    {
        void Set(string key, object value, int timeInSecond);
        object Get(string key);
        void ClearCache();
    }
}

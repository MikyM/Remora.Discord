//
//  EvictionCachingCacheService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Results;

namespace Remora.Discord.Caching.Services;

/// <inheritdoc cref="CacheService"/>
/// <inheritdoc cref="IEvictionCachingCacheService"/>
/// <summary>
/// Handles cache insert/evict operations for various types and supports caching evicted values.
/// </summary>
[PublicAPI]
public class EvictionCachingCacheService : CacheService, IEvictionCachingCacheService
{
    private readonly IEvictionCachingCacheProvider _evictionCachingCacheProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictionCachingCacheService"/> class.
    /// </summary>
    /// <param name="evictionCachingCacheProvider">The eviction caching cache provider.</param>
    /// <param name="cacheSettings">The cache settings.</param>
    public EvictionCachingCacheService(IEvictionCachingCacheProvider evictionCachingCacheProvider, IOptions<CacheSettings> cacheSettings)
        : base(evictionCachingCacheProvider, cacheSettings)
    {
        _evictionCachingCacheProvider = evictionCachingCacheProvider;
    }

    /// <inheritdoc cref="IEvictionCachingCacheService.EvictAsync{TInstance}"/>
    public override async ValueTask<Result<TInstance>> EvictAsync<TInstance>(string key, CancellationToken ct = default)
    {
        var options = this.CacheSettings.GetEvictionEntryOptions<TInstance>();

        return await _evictionCachingCacheProvider.EvictAndCacheAsync<TInstance>
            (
                key,
                KeyHelpers.CreateEvictionCacheKey(key),
                options.AbsoluteExpiration,
                options.SlidingExpiration,
                ct
            );
    }

    /// <inheritdoc cref="IEvictionCachingCacheService.TryGetPreviousValueAsync{TInstance}"/>
    public ValueTask<Result<TInstance>> TryGetPreviousValueAsync<TInstance>(string key)
        where TInstance : class => _evictionCachingCacheProvider.RetrieveAsync<TInstance>(KeyHelpers.CreateEvictionCacheKey(key));
}

//
//  IEvictionCachingCacheProvider.cs
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

using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Results;

namespace Remora.Discord.Caching.Abstractions.Services;

/// <inheritdoc cref="ICacheProvider"/>
/// <summary>
/// Represents an abstraction between a cache service and it's backing store and supports caching evicted values.
/// </summary>
[PublicAPI]
public interface IEvictionCachingCacheProvider : ICacheProvider
{
    /// <summary>
    /// Evicts a key from the backing store and re-caches it as an atomic operation.
    /// </summary>
    /// <param name="key">The key to evict from the backing store.</param>
    /// <param name="evictedKey">The evicted key to be used in order to re-cache.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the value to cache.</param>
    /// <param name="slidingExpiration">The sliding expiration of the value to cache.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the result of the potentially asynchronous action.</returns>
    ValueTask<Result> EvictAndCacheAsync
    (
        string key,
        string evictedKey,
        DateTimeOffset? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Evicts a key from the backing store and re-caches it, returning its current value if it exists as an atomic operation.
    /// </summary>
    /// <param name="key">The key to evict from the backing store.</param>
    /// <param name="evictedKey">The evicted key to be used in order to re-cache.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the value to cache.</param>
    /// <param name="slidingExpiration">The sliding expiration of the value to cache.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <typeparam name="TInstance">The type to return from the backing store, if it exists.</typeparam>
    /// <returns>A <see cref="ValueTask"/> representing the result of the potentially asynchronous action.</returns>
    ValueTask<Result<TInstance>> EvictAndCacheAsync<TInstance>
    (
        string key,
        string evictedKey,
        DateTimeOffset? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken ct = default
    )
        where TInstance : class;
}

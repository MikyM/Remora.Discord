//
//  ICacheService.cs
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
using Remora.Results;

namespace Remora.Discord.Caching.Abstractions.Services;

/// <summary>
/// Represents a cache service that handles cache insert/evict operations for various types.
/// </summary>
[PublicAPI]
public interface ICacheService
{
    /// <summary>
    /// Caches a value. Certain instance types may have specializations which cache more than one value from the
    /// instance.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <returns>A <see cref="ValueTask"/> representing the potentially asynchronous operation.</returns>
    ValueTask CacheAsync<TInstance>(string key, TInstance instance, CancellationToken ct = default)
        where TInstance : class;

    /// <summary>
    /// Attempts to retrieve a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <returns>A <see cref="Result"/> that may or not have succeeded.</returns>
    ValueTask<Result<TInstance>> TryGetValueAsync<TInstance>(string key, CancellationToken ct = default)
        where TInstance : class;

    /// <summary>
    /// Evicts the instance with the given key from the cache.
    /// </summary>
    /// <typeparam name="TInstance">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the potentially asynchronous operation.</returns>
    ValueTask<Result<TInstance>> EvictAsync<TInstance>(string key, CancellationToken ct = default)
        where TInstance : class;
}

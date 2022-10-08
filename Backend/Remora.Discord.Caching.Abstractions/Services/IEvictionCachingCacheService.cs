//
//  IEvictionCachingCacheService.cs
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

using System.Threading.Tasks;
using JetBrains.Annotations;
using Remora.Results;

namespace Remora.Discord.Caching.Abstractions.Services;

/// <inheritdoc cref="ICacheService"/>
/// <summary>
/// Represents a cache service that handles cache insert/evict operations for various types and supports caching evicted values.
/// </summary>
[PublicAPI]
public interface IEvictionCachingCacheService : ICacheService
{
    /// <summary>
    /// Attempts to retrieve the previous value of the given key from the eviction cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <returns>A <see cref="Result"/> that may or not have succeeded.</returns>
    ValueTask<Result<TInstance>> TryGetPreviousValueAsync<TInstance>(string key)
        where TInstance : class;
}

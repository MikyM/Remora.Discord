//
//  LuaScripts.cs
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

namespace Remora.Discord.Caching.Redis;

/// <summary>
/// Lua scripts for atomic redis operations.
/// </summary>
internal static class LuaScripts
{
    /// <summary>
    /// Script that DELSs the key only if it exists, re-caches the key's value as an "evicted" entry, can return the just evicted value.
    /// </summary>
    /// <remarks>
    /// <para> KEYS[1] = key.</para>
    /// <para> ARGV[1] = absolute-expiration - Unix timestamp in seconds as long (-1 for none).</para>
    /// <para> ARGV[2] = sliding-expiration - number of seconds as long (-1 for none).</para>
    /// <para> ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration).</para>
    /// <para> ARGV[4] = whether to return existing data if applies - 0 for no or 1 for yes.</para>
    /// <para> ARGV[5] = evicted key.</para>
    /// <para><b> This order should not change as the LUA script depends on it.</b></para>
    /// </remarks>
    internal const string EvictAndCacheScript = @"
                local result = redis.call('HGET', KEYS[1], 'data')
                if result == false then
                  return nil
                end

                redis.call('DEL', KEYS[1])

                redis.call('HSET', ARGV[5], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', result)

                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', ARGV[5], ARGV[3])
                end 

                if ARGV[1] == '1' then
                  return result['data']
                end
                return '1'";

    /// <summary>
    /// Script that DELSs the key only if it exists, can return the just evicted value.
    /// </summary>
    /// <remarks>
    /// <para> KEYS[1] = key.</para>
    /// <para> ARGV[1] = whether to return existing data if applies - 0 for no or 1 for yes.</para>
    /// <para><b> This order should not change as the LUA script depends on it.</b></para>
    /// </remarks>
    internal const string EvictScript = @"
                local result = redis.call('HGET', KEYS[1], 'data')
                if result == false then
                  return nil
                end

                redis.call('DEL', KEYS[1])

                if ARGV[1] == '1' then
                  return result['data']
                end
                return '1'";

    /// <summary>
    /// Script that HSETs the key's value with expiration data, returns "1" if successfully set.
    /// </summary>
    /// <remarks>
    /// <para> KEYS[1] = key.</para>
    /// <para> ARGV[1] = absolute-expiration - Unix timestamp in seconds as long (-1 for none).</para>
    /// <para> ARGV[2] = sliding-expiration - number of seconds as long (-1 for none).</para>
    /// <para>  ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration).</para>
    /// <para>  ARGV[4] = data - serialized JSON.</para>
    /// <para><b> This order should not change as the LUA script depends on it.</b></para>
    /// </remarks>
    internal const string SetScript = @"
                redis.call('HSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end 
                return '1'";

    /// <summary>
    /// Script that HGETs the key's value only if it exists, refreshes the expiration, can return the value or not.
    /// </summary>
    /// <remarks>
    /// <para> KEYS[1] = key.</para>
    /// <para> ARGV[1] = whether to return data or only refresh - 0 for no data, 1 to return data.</para>
    /// <para><b> This order should not change as the LUA script depends on it.</b></para>
    /// </remarks>
    internal const string GetAndRefreshScript = @"
                local sub = function (key)
                  local bulk = redis.call('HGETALL', key)
	                 local result = {}
	                 local nextkey
	                 for i, v in ipairs(bulk) do
		                 if i % 2 == 1 then
			                 nextkey = v
		                 else
			                 result[nextkey] = v
		                 end
	                 end
	                 return result
                end

                local result = sub(KEYS[1])
                if next(result) == nil then
                  return nil
                end

                local sldexp = tonumber(result['sldexp'])
                local absexp = tonumber(result['absexp'])

                if sldexp == -1 then
                  if ARGV[1] == '1' then
                    return result['data']
                  else
                    return '1'
                  end
                end

                local exp = 1

                local time = tonumber(redis.call('TIME')[1])
                if absexp ~= -1 then
                  local relexp = absexp - time
                  if relexp <= sldexp then
                    exp = relexp
                  else
                    exp = sldexp                   
                  end
                else
                  exp = sldexp
                end
                
                redis.call('EXPIRE', KEYS[1], exp, 'XX')
                                
                if ARGV[1] == '1' then
                  return result['data']
                end
                return '1'";

    /// <summary>
    /// The value passed to a script when returning the data is desired.
    /// </summary>
    internal const string ReturnDataArg = "1";

    /// <summary>
    /// The value returned from a script indicating a successful execution.
    /// </summary>
    internal const string SuccessfulScriptResult = "1";

    /// <summary>
    /// The value passed to a script when not returning the data is desired.
    /// </summary>
    internal const string DontReturnDataArg = "0";

    /// <summary>
    /// The value passed to a script when data is not present.
    /// </summary>
    internal const long NotPresentArg = -1;
}

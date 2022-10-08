//
//  CacheService.cs
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
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Results;

namespace Remora.Discord.Caching.Services;

/// <inheritdoc cref="ICacheService"/>
/// <summary>
/// Handles cache insert/evict operations for various types.
/// </summary>
[PublicAPI]
public class CacheService : ICacheService
{
    private readonly ICacheProvider _cacheProvider;

    /// <summary>
    /// Gets the cache settings.
    /// </summary>
    protected CacheSettings CacheSettings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    /// <param name="cacheProvider">The cache provider.</param>
    /// <param name="cacheSettings">The cache settings.</param>
    public CacheService(ICacheProvider cacheProvider, IOptions<CacheSettings> cacheSettings)
    {
        _cacheProvider = cacheProvider;
        this.CacheSettings = cacheSettings.Value;
    }

    /// <inheritdoc cref="ICacheService.CacheAsync{TInstance}"/>
    public async ValueTask CacheAsync<TInstance>(string key, TInstance instance, CancellationToken ct = default)
        where TInstance : class
    {
        if (this.CacheSettings.GetAbsoluteExpirationOrDefault(typeof(TInstance)) is var absoluteExpiration)
        {
            if (absoluteExpiration == TimeSpan.Zero)
            {
                return;
            }
        }

        Func<ValueTask> cacheAction = instance switch
        {
            IWebhook webhook => () => CacheWebhookAsync(key, webhook),
            ITemplate template => () => CacheTemplateAsync(key, template),
            IIntegration integration => () => CacheIntegrationAsync(key, integration),
            IBan ban => () => CacheBanAsync(key, ban),
            IGuildMember member => () => CacheGuildMemberAsync(key, member),
            IGuildPreview preview => () => CacheGuildPreviewAsync(key, preview),
            IGuild guild => () => CacheGuildAsync(key, guild),
            IEmoji emoji => () => CacheEmojiAsync(key, emoji),
            IInvite invite => () => CacheInviteAsync(key, invite),
            IMessage message => () => CacheMessageAsync(key, message),
            IChannel channel => () => CacheChannelAsync(key, channel),
            _ => () => CacheInstanceAsync(key, instance)
        };

        await cacheAction();
    }

    /// <inheritdoc cref="ICacheService.TryGetValueAsync{TInstance}"/>
    public ValueTask<Result<TInstance>> TryGetValueAsync<TInstance>(string key, CancellationToken ct = default)
        where TInstance : class => _cacheProvider.RetrieveAsync<TInstance>(key, ct);

    /// <inheritdoc cref="ICacheService.EvictAsync{TInstance}"/>
    public virtual async ValueTask<Result<TInstance>> EvictAsync<TInstance>(string key, CancellationToken ct = default)
        where TInstance : class => await _cacheProvider.EvictAsync<TInstance>(key, ct);

    private async ValueTask CacheWebhookAsync(string key, IWebhook webhook)
    {
        await CacheInstanceAsync(key, webhook);

        if (!webhook.User.IsDefined(out var user))
        {
            return;
        }

        var userKey = KeyHelpers.CreateUserCacheKey(user.ID);
        await CacheAsync(userKey, user);
    }

    private async ValueTask CacheTemplateAsync(string key, ITemplate template)
    {
        await CacheInstanceAsync(key, template);

        var creatorKey = KeyHelpers.CreateUserCacheKey(template.Creator.ID);
        await CacheAsync(creatorKey, template.Creator);
    }

    private async ValueTask CacheIntegrationAsync(string key, IIntegration integration)
    {
        await CacheInstanceAsync(key, integration);

        if (!integration.User.IsDefined(out var user))
        {
            return;
        }

        var userKey = KeyHelpers.CreateUserCacheKey(user.ID);
        await CacheAsync(userKey, user);
    }

    private async ValueTask CacheBanAsync(string key, IBan ban)
    {
        await CacheInstanceAsync(key, ban);

        var userKey = KeyHelpers.CreateUserCacheKey(ban.User.ID);
        await CacheAsync(userKey, ban.User);
    }

    private async ValueTask CacheGuildMemberAsync(string key, IGuildMember member)
    {
        await CacheInstanceAsync(key, member);

        if (!member.User.IsDefined(out var user))
        {
            return;
        }

        var userKey = KeyHelpers.CreateUserCacheKey(user.ID);
        await CacheAsync(userKey, user);
    }

    private async ValueTask CacheGuildPreviewAsync(string key, IGuildPreview preview)
    {
        await CacheInstanceAsync(key, preview);

        foreach (var emoji in preview.Emojis)
        {
            if (emoji.ID is null)
            {
                continue;
            }

            var emojiKey = KeyHelpers.CreateEmojiCacheKey(preview.ID, emoji.ID.Value);
            await CacheAsync(emojiKey, emoji);
        }
    }

    private async ValueTask CacheGuildAsync(string key, IGuild guild)
    {
        await CacheInstanceAsync(key, guild);

        foreach (var emoji in guild.Emojis)
        {
            if (emoji.ID is null)
            {
                continue;
            }

            var emojiKey = KeyHelpers.CreateEmojiCacheKey(guild.ID, emoji.ID.Value);
            await CacheAsync(emojiKey, emoji);
        }

        var rolesKey = KeyHelpers.CreateGuildRolesCacheKey(guild.ID);
        await CacheAsync(rolesKey, guild.Roles);

        foreach (var role in guild.Roles)
        {
            var roleKey = KeyHelpers.CreateGuildRoleCacheKey(guild.ID, role.ID);
            await CacheAsync(roleKey, role);
        }
    }

    private async ValueTask CacheEmojiAsync(string key, IEmoji emoji)
    {
        await CacheInstanceAsync(key, emoji);

        if (!emoji.User.IsDefined(out var creator))
        {
            return;
        }

        var creatorKey = KeyHelpers.CreateUserCacheKey(creator.ID);
        await CacheAsync(creatorKey, creator);
    }

    private async ValueTask CacheInviteAsync(string key, IInvite invite)
    {
        await CacheInstanceAsync(key, invite);

        if (!invite.Inviter.IsDefined(out var inviter))
        {
            return;
        }

        var inviterKey = KeyHelpers.CreateUserCacheKey(inviter.ID);
        await CacheAsync(inviterKey, inviter);
    }

    private async ValueTask CacheMessageAsync(string key, IMessage message)
    {
        await CacheInstanceAsync(key, message);

        var authorKey = KeyHelpers.CreateUserCacheKey(message.Author.ID);
        await CacheAsync(authorKey, message.Author);

        if (!message.ReferencedMessage.IsDefined(out var referencedMessage))
        {
            return;
        }

        var referencedMessageKey = KeyHelpers.CreateMessageCacheKey
        (
            referencedMessage.ChannelID,
            referencedMessage.ID
        );

        await CacheAsync(referencedMessageKey, referencedMessage);
    }

    private async ValueTask CacheChannelAsync(string key, IChannel channel)
    {
        await CacheInstanceAsync(key, channel);
        if (!channel.Recipients.IsDefined(out var recipients))
        {
            return;
        }

        foreach (var recipient in recipients)
        {
            var recipientKey = KeyHelpers.CreateUserCacheKey(recipient.ID);
            await CacheAsync(recipientKey, recipient);
        }
    }

    /// <summary>
    /// Caches an instance using the default entry options for that instance type.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="instance">The instance.</param>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    private ValueTask CacheInstanceAsync<TInstance>(string key, TInstance instance)
        where TInstance : class
    {
        var options = this.CacheSettings.GetEntryOptions<TInstance>();
        return _cacheProvider.CacheAsync(key, instance, options.AbsoluteExpiration, options.SlidingExpiration);
    }
}

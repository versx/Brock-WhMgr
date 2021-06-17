namespace WhMgr.Commands
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;

    using WhMgr.Configuration;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Localization;

    public class Quests : BaseCommandModule
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("QUESTS", Program.LogLevel);
        private readonly WhConfigHolder _config;

        public Quests(WhConfigHolder config)
        {
            _config = config;
        }

        [
            Command("reset-quests"),
            Hidden,
            RequirePermissions(Permissions.KickMembers)
        ]
        public async Task ResetChannelAsync(CommandContext ctx,
            [Description("Discord channel to reset.")] DiscordChannel channel = null)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));

            if (channel == null)
            {
                var channelIds = _config.Instance.Servers[guildId].QuestChannelIds;
                for (var i = 0; i < channelIds.Count; i++)
                {
                    var qChannel = await ctx.Client.GetChannelAsync(channelIds[i]);
                    if (qChannel == null)
                    {
                        _logger.Warn($"Unable to get quest channel from id '{channelIds[i]}'.");
                        continue;
                    }

                    await DeleteChannelMessages(ctx, qChannel);
                }
                return;
            }

            await DeleteChannelMessages(ctx, channel);
        }

        private async Task DeleteChannelMessages(CommandContext ctx, DiscordChannel channel)
        {
            var messages = await channel.GetMessagesAsync();
            while (messages.Count > 0)
            {
                for (var j = 0; j < messages.Count; j++)
                {
                    var message = messages[j];
                    if (message == null)
                        continue;

                    await message.DeleteAsync("Channel reset.");
                    Thread.Sleep(100);
                }

                messages = await channel.GetMessagesAsync();
            }
            await ctx.RespondEmbed(Translator.Instance.Translate("CHANNEL_MESSAGES_DELETED").FormatText(ctx.User.Username, channel.Mention));
        }
    }
}
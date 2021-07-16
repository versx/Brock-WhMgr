namespace WhMgr.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.Entities;
    using DSharpPlus.Interactivity;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using WhMgr.Configuration;
    using WhMgr.Diagnostics;
    using WhMgr.Localization;
    using WhMgr.Net.Models;

    public static class DiscordExtensions
    {
        public const string ConfirmRegex = "\\b[Yy][Ee]?[Ss]?\\b|\\b[Nn][Oo]?\\b";
        public const string YesRegex = "[Yy][Ee]?[Ss]?";
        //private const string NoRegex = "[Nn][Oo]?";

        private static readonly IEventLogger _logger = EventLogger.GetLogger("DISCORD_EXTENSIONS", Program.LogLevel);

        #region Messages

        public static async Task<List<DiscordMessage>> RespondEmbed(this DiscordMessage msg, string message)
        {
            return await msg.RespondEmbed(message, DiscordColor.Green);
        }

        public static async Task<List<DiscordMessage>> RespondEmbed(this DiscordMessage discordMessage, string message, DiscordColor color)
        {
            var messagesSent = new List<DiscordMessage>();
            var messages = message.SplitInParts(2048);
            foreach (var msg in messages)
            {
                var eb = new DiscordEmbedBuilder
                {
                    Color = color,
                    Description = msg
                };

                messagesSent.Add(await discordMessage.RespondAsync(embed: eb));
                Thread.Sleep(500);
            }
            return messagesSent;
        }

        public static async Task<List<DiscordMessage>> RespondEmbed(this CommandContext ctx, string message)
        {
            return await RespondEmbed(ctx, message, DiscordColor.Green);
        }

        public static async Task<List<DiscordMessage>> RespondEmbed(this CommandContext ctx, string message, DiscordColor color)
        {
            var messagesSent = new List<DiscordMessage>();
            var messages = message.SplitInParts(2048);
            foreach (var msg in messages)
            {
                var eb = new DiscordEmbedBuilder
                {
                    Color = color,
                    Description = msg
                };

                await ctx.TriggerTypingAsync();
                messagesSent.Add(await ctx.RespondAsync(embed: eb));
            }
            return messagesSent;
        }

        public static async Task<DiscordMessage> SendDirectMessage(this DiscordClient client, DiscordUser user, DiscordEmbed embed)
        {
            if (embed == null)
                return null;

            return await client.SendDirectMessage(user, string.Empty, embed);
        }

        public static async Task<DiscordMessage> SendDirectMessage(this DiscordClient client, DiscordUser user, string message, DiscordEmbed embed)
        {
            try
            {
                var dm = await client.CreateDmAsync(user);
                if (dm != null)
                {
                    var msg = await dm.SendMessageAsync(message, false, embed);
                    return msg;
                }
            }
            catch (Exception)
            {
                //_logger.Error(ex);
                _logger.Error($"Failed to send DM to user {user.Username}.");
            }

            return null;
        }

        #endregion

        private static readonly Dictionary<(ulong, ulong), Task<DiscordMember>> MemberTasks = new Dictionary<(ulong, ulong), Task<DiscordMember>>();

        public static async Task<DiscordMember> GetMemberById(this DiscordClient client, ulong guildId, ulong id)
        {
            Task<DiscordMember> taskToAwait;
            var added = false;

            lock (MemberTasks)
            {
                if (MemberTasks.TryGetValue((guildId, id), out var existingTask))
                {
                    taskToAwait = existingTask;
                }
                else
                {
                    taskToAwait = DoGetMemberById(client, guildId, id);
                    MemberTasks.Add((guildId, id), taskToAwait);
                    added = true;
                }
            }

            var result = await taskToAwait;

            if (added)
            {
                lock (MemberTasks)
                {
                    MemberTasks.Remove((guildId, id));
                }
            }

            return result;
        }

        private static async Task<DiscordMember> DoGetMemberById(DiscordClient client, ulong guildId, ulong id)
		{
            if (!client.Guilds.ContainsKey(guildId))
                return null;

            var guild = client.Guilds[guildId];
            if (guild == null)
                return null;

            var members = guild.Members;
            if (members?.Count <= 0)
                return null;

            DiscordMember member = null;
            try
            {
                member = members?.FirstOrDefault(x => x.Id == id);
            }
            catch { }
            if (member == null)
            {
                try
                {
                    member = await guild.GetMemberAsync(id);
                }
                catch
                {
                    return null;
                }
            }

            return member;
        }

        public static async Task<DiscordMessage> DonateUnlockFeaturesMessage(this CommandContext ctx, bool triggerTyping = true)
        {
            if (triggerTyping)
            {
                await ctx.TriggerTypingAsync();
            }

            var message = Translator.Instance.Translate("DONATE_MESSAGE", new { author = ctx.User.Username }) ??
                $"{ctx.User.Username} This feature is only available to supporters, please $donate to unlock this feature and more.\r\n\r\n" +
                $"Donation information can be found by typing the `$donate` command.\r\n\r\n" +
                $"*If you have already donated and are still receiving this message, please tag an Administrator or Moderator for help.*";
            var eb = await ctx.RespondEmbed(message);
            return eb.FirstOrDefault();
        }

        internal static async Task<bool> IsDirectMessageSupported(this CommandContext ctx, WhConfig config)
        {
            var exists = ctx.Client.Guilds.Keys.FirstOrDefault(x => config.Servers.ContainsKey(x)) > 0;
            if (!exists)
            {
                await ctx.Message.RespondEmbed(Translator.Instance.Translate("DIRECT_MESSAGE_NOT_SUPPORTED", new { author = ctx.Message.Author.Username }), DiscordColor.Yellow);
                return false;
            }

            return true;
        }

        public static ulong ContextToGuild(this CommandContext ctx, Dictionary<ulong, DiscordClient> servers)
        {
            foreach (var (guildId, client) in servers)
            {
                if (ctx.Client.CurrentUser.Id != client.CurrentUser.Id)
                    continue;

                return guildId;
            }
            return 0;
        }

        #region Roles

        public static async Task<bool> IsSupporterOrHigher(this DiscordClient client, ulong userId, ulong guildId, WhConfig config)
        {
            try
            {
                if (!config.Servers.ContainsKey(guildId))
                    return false;

                var server = config.Servers[guildId];

                var isAdmin = userId == server.OwnerId;
                if (isAdmin)
                    return true;

                var isModerator = await IsModerator(client, userId, guildId, config);
                if (isModerator)
                    return true;

                var isSupporter = client.HasSupporterRole(guildId, userId, server.DonorRoleIds);
                if (isSupporter)
                    return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return false;
        }

        public static async Task<bool> IsModerator(this DiscordClient client, ulong userId, ulong guildId, WhConfig config)
        {
            if (!config.Servers.ContainsKey(guildId))
                return false;

            var server = config.Servers[guildId];
            var moderatorRoleIds = server.ModeratorRoleIds;
            var member = await client.GetMemberById(guildId, userId);
            if (member == null)
                return false;

            var roleIds = member.Roles.Select(x => x.Id);
            foreach (var modRoleId in moderatorRoleIds)
            {
                if (roleIds.Contains(modRoleId))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<bool> IsModeratorOrHigher(this DiscordClient client, ulong userId, ulong guildId, WhConfig config)
        {
            if (!config.Servers.ContainsKey(guildId))
                return false;

            var server = config.Servers[guildId];

            var isAdmin = IsAdmin(userId, server.OwnerId);
            if (isAdmin)
                return true;

            var isModerator = await IsModerator(client, userId, guildId, config);
            if (isModerator)
                return true;

            return false;
        }

        public static bool IsAdmin(this ulong userId, ulong ownerId)
        {
            return userId == ownerId;
        }

        public static bool HasSupporterRole(this DiscordClient client, ulong guildId, ulong userId, List<ulong> supporterRoleIds)
        {
            if (!client.Guilds.ContainsKey(guildId))
                return false;

            var guild = client.Guilds[guildId];
            var member = guild.Members.FirstOrDefault(x => x.Id == userId);
            if (member == null)
            {
                _logger.Error($"Failed to get user with id {userId}.");
                return false;
            }

            return member.HasSupporterRole(supporterRoleIds);
        }

        public static bool HasSupporterRole(this DiscordMember member, List<ulong> supporterRoleIds)
        {
            for (var i = 0; i < supporterRoleIds.Count; i++)
            {
                if (HasRole(member, supporterRoleIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> HasModeratorRole(this DiscordClient client, ulong guildId, ulong userId, ulong moderatorRoleId)
        {
            var member = await client.GetMemberById(guildId, userId);
            if (member == null)
            {
                _logger.Error($"Failed to get moderator user with id {userId}.");
                return false;
            }

            return member.HasModeratorRole(moderatorRoleId);
        }

        public static bool HasModeratorRole(this DiscordMember member, ulong moderatorRoleId)
        {
            return HasRole(member, moderatorRoleId);
        }

        public static bool HasRole(this DiscordMember member, ulong roleId)
        {
            try
            {
                var role = member?.Roles.FirstOrDefault(x => x.Id == roleId);
                return role != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasRole(this DiscordGuild guild, DiscordMember member, string roleName)
        {
            var role = guild.GetRoleFromName(roleName);
            if (role == null) return false;

            return HasRole(member, role.Id);
        }

        public static DiscordRole GetRoleFromName(this DiscordGuild guild, string roleName)
        {
            return guild?.Roles.FirstOrDefault(x => string.Compare(x.Name, roleName, true) == 0);
        }

        #endregion

        public static async Task<Tuple<DiscordChannel, long>> DeleteMessages(this DiscordClient client, ulong channelId)
        {
            var deleted = 0L;
            DiscordChannel channel;
            try
            {
                channel = await client.GetChannelAsync(channelId);
            }
            catch (DSharpPlus.Exceptions.NotFoundException)
            {
                _logger.Debug($"Failed to get Discord channel {channelId}, skipping...");
                return null;
            }

            if (channel == null)
            {
                _logger.Warn($"Failed to find channel by id {channelId}, skipping...");
                return null;
            }

            var messages = await channel?.GetMessagesAsync();
            if (messages == null)
                return null;

            while (messages.Count > 0)
            {
                for (var j = 0; j < messages.Count; j++)
                {
                    var message = messages[j];
                    if (message == null)
                        continue;

                    try
                    {
                        await message.DeleteAsync("Channel reset.");
                        deleted++;
                    }
                    catch { continue; }
                }

                try
                {
                    messages = await channel.GetMessagesAsync();
                }
                catch (Newtonsoft.Json.JsonReaderException ex)
                {
                    _logger.Error(ex);
                    continue;
                }
            }

            return Tuple.Create(channel, deleted);
        }

        public static async Task<bool> Confirm(this CommandContext ctx, string message)
        {
            await ctx.RespondEmbed(message);
            var interactivity = ctx.Client.GetModule<InteractivityModule>();
            if (interactivity == null)
            {
                _logger.Error("Interactivity model failed to load!");
                return false;
            }

            var m = await interactivity.WaitForMessageAsync(
                x => x.Channel.Id == ctx.Channel.Id
                && x.Author.Id == ctx.User.Id
                && Regex.IsMatch(x.Content, ConfirmRegex), 
                TimeSpan.FromMinutes(2));

            return Regex.IsMatch(m.Message.Content, YesRegex);
        }

        #region Colors

        public static DiscordColor BuildPokemonIVColor(this string iv, DiscordEmbedColorConfig config)
        {
            if (!double.TryParse(iv.Substring(0, iv.Length - 1), out var result))
            {
                return DiscordColor.White;
            }
            var color = config.Pokemon.IV.FirstOrDefault(x => result >= x.Minimum && result <= x.Maximum);
            return new DiscordColor(color.Color);
        }

        public static DiscordColor BuildPokemonPvPColor(this int rank, DiscordEmbedColorConfig config)
        {
            if (rank <= 0)
            {
                return DiscordColor.White;
            }
            var color = config.Pokemon.PvP.FirstOrDefault(x => rank >= x.Minimum && rank <= x.Maximum);
            return new DiscordColor(color.Color);
        }

        public static DiscordColor BuildRaidColor(this int level, DiscordEmbedColorConfig config)
{
            if (level == 0)
            {
                return DiscordColor.White;
            }
            string color;
            switch (level)
            {
                case 1:
                    color = config.Raids.Level1;
                    break;
                case 2:
                    color = config.Raids.Level2;
                    break;
                case 3:
                    color = config.Raids.Level3;
                    break;
                case 4:
                    color = config.Raids.Level4;
                    break;
                case 5:
                    color = config.Raids.Level5;
                    break;
                case 6:
                    color = config.Raids.Level6;
                    break;
                default:
                    color = config.Raids.Ex;
                    break;
            }
            return new DiscordColor(color);
        }

        public static DiscordColor BuildLureColor(this PokestopLureType lureType, DiscordEmbedColorConfig config)
        {
            string color;
            switch (lureType)
            {
                case PokestopLureType.Normal:
                    color = config.Pokestops.Lures.Normal;
                    break;
                case PokestopLureType.Glacial:
                    color = config.Pokestops.Lures.Glacial;
                    break;
                case PokestopLureType.Mossy:
                    color = config.Pokestops.Lures.Mossy;
                    break;
                case PokestopLureType.Magnetic:
                    color = config.Pokestops.Lures.Magnetic;
                    break;
                case PokestopLureType.Rainy:
                    color = config.Pokestops.Lures.Rainy;
                    break;
                default:
                    return DiscordColor.White;
            }
            return new DiscordColor(color);
        }

        public static DiscordColor BuildWeatherColor(this WeatherCondition weather, DiscordEmbedColorConfig config)
        {
            var color = "#808080";
            switch (weather)
            {
                case WeatherCondition.Clear:
                    color = config.Weather.Clear;
                    break;
                case WeatherCondition.Overcast:
                    color = config.Weather.Cloudy;
                    break;
                case WeatherCondition.Fog:
                    color = config.Weather.Fog;
                    break;
                case WeatherCondition.PartlyCloudy:
                    color = config.Weather.PartlyCloudy;
                    break;
                case WeatherCondition.Rainy:
                    color = config.Weather.Rain;
                    break;
                case WeatherCondition.Snow:
                    color = config.Weather.Snow;
                    break;
                case WeatherCondition.Windy:
                    color = config.Weather.Windy;
                    break;
            }
            return new DiscordColor(color);
        }

        #endregion
    }
}
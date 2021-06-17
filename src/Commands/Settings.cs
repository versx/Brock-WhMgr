namespace WhMgr.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;

    using WhMgr.Configuration;
    using WhMgr.Extensions;

    [
        Group("settings"),
        Aliases("config", "cfg", "conf", "c"),
        Description("Event Pokemon management commands."),
        Hidden,
        RequirePermissions(Permissions.KickMembers)
    ]
    public class Settings : BaseCommandModule
    {
        private readonly WhConfigHolder _config;

        public Settings(WhConfigHolder config)
        {
            _config = config;
        }

        [
            Command("set"),
            Aliases("s"),
            Description("")
        ]
        public async Task SetAsync(CommandContext ctx,
            [Description("")] string key,
            [Description("")] string value)
        {
            // TODO: Provide list of available config options to set.
            if (!await ctx.IsDirectMessageSupported(_config.Instance))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));

            if (!_config.Instance.Servers.ContainsKey(guildId))
            {
                // TODO: Localize
                await ctx.RespondEmbed($"{ctx.User.Username} Guild {ctx.Guild?.Name} ({guildId}) not configured in {Strings.ConfigFileName}");
                return;
            }

            //var guildConfig = _config.Instance.Servers[guildId];
            switch (key)
            {
                case "nest_channel":
                    // TODO: Validate nestChannelId
                    //_config.Instance.Servers[guildId].NestsChannelId = value;
                    //_config.Instance.Save(_config.Instance.FileName);
                    break;
                case "prefix":
                    var oldPrefix = _config.Instance.Servers[guildId].CommandPrefix;
                    await ctx.RespondEmbed($"{ctx.User.Username} Command prefix changed from {oldPrefix} to {value}.", DiscordColor.Green);
                    _config.Instance.Servers[guildId].CommandPrefix = value;
                    _config.Instance.Save(_config.Instance.FileName);
                    break;
                case "enable_subscriptions":
                    if (!bool.TryParse(value, out var enableSubscriptions))
                    {
                        await ctx.RespondEmbed($"{ctx.User.Username}", DiscordColor.Red);
                        return;
                    }
                    _config.Instance.Servers[guildId].Subscriptions.Enabled = enableSubscriptions;
                    _config.Instance.Save(_config.Instance.FileName);
                    break;
                case "cities_require_donor":
                    if (!bool.TryParse(value, out var citiesRequireDonor))
                    {
                        await ctx.RespondEmbed($"{ctx.User.Username}", DiscordColor.Red);
                        return;
                    }
                    _config.Instance.Servers[guildId].CitiesRequireSupporterRole = citiesRequireDonor;
                    _config.Instance.Save(_config.Instance.FileName);
                    break;
                case "prune_quests":
                    if (!bool.TryParse(value, out var pruneQuests))
                    {
                        await ctx.RespondEmbed($"{ctx.User.Username}", DiscordColor.Red);
                        return;
                    }
                    _config.Instance.Servers[guildId].PruneQuestChannels = pruneQuests;
                    _config.Instance.Save(_config.Instance.FileName);
                    break;
                case "icon_style":
                    if (!_config.Instance.IconStyles.ContainsKey(value))
                    {
                        await ctx.RespondEmbed($"{ctx.User.Username}", DiscordColor.Red);
                        return;
                    }
                    _config.Instance.Servers[guildId].IconStyle = value;
                    _config.Instance.Save(_config.Instance.FileName);
                    break;
                case "shiny_stats":
                    if (!bool.TryParse(value, out var enableShinyStats))
                    {
                        await ctx.RespondEmbed($"{ctx.User.Username}", DiscordColor.Red);
                        return;
                    }
                    _config.Instance.Servers[guildId].ShinyStats.Enabled = enableShinyStats;
                    _config.Instance.Save(_config.Instance.FileName);
                    break;
            }
            await Task.CompletedTask;
        }

        [
            Command("list"),
            Aliases("l"),
            Description("List config settings for current guild.")
        ]
        public async Task ListSettingsAsync(CommandContext ctx)
        {
            if (!await ctx.IsDirectMessageSupported(_config.Instance))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));

            if (!_config.Instance.Servers.ContainsKey(ctx.Guild?.Id ?? 0))
            {
                // TODO: Localize
                await ctx.RespondEmbed($"{ctx.User.Username} Guild {ctx.Guild?.Name} ({guildId}) not configured in {Strings.ConfigFileName}");
                return;
            }

            var guildConfig = _config.Instance.Servers[guildId];
            var eb = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Blurple,
                Title = $"{ctx.Guild.Name} Config",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{ctx.Guild?.Name} | {DateTime.Now}",
                    IconUrl = ctx.Guild?.IconUrl
                }
            };

            // TODO: Localize
            eb.AddField($"City Roles", string.Join("\r\n", guildConfig.Geofences.Select(x => x.Name)), true);
            eb.AddField($"Enable Subscriptions", guildConfig.Subscriptions.Enabled ? "Yes" : "No", true);
            eb.AddField($"Command Prefix", guildConfig.CommandPrefix ?? "@BotMentionHere", true);
            eb.AddField($"City Roles Require Donor Role", guildConfig.CitiesRequireSupporterRole ? "Yes" : "No", true);
            eb.AddField($"Donor Roles", string.Join("\r\n", guildConfig.DonorRoleIds.Select(x => $"{ctx.Guild.GetRole(x).Name}:{x}")), true);
            // TODO: Use await
            //eb.AddField($"Moderators", string.Join("\r\n", guildConfig.ModeratorRoleIds.Select(x => $"{ctx.Client.GetMemberById(guildId, x).GetAwaiter().GetResult().Username}:{x}")), true);
            eb.AddField($"Nest Channel", guildConfig.NestsChannelId == 0 ? "Not Set" : $"{ctx.Guild.GetChannel(guildConfig.NestsChannelId)?.Name}:{guildConfig.NestsChannelId}", true);
            eb.AddField($"Prune Quest Channels", guildConfig.PruneQuestChannels ? "Yes" : "No", true);
            eb.AddField($"Quest Channels", string.Join("\r\n", guildConfig.QuestChannelIds.Select(x => $"{ctx.Guild.GetChannel(x)?.Name}:{x}")), true);
            eb.AddField($"Enable Shiny Stats", guildConfig.ShinyStats?.Enabled ?? false ? "Yes" : "No", true);
            eb.AddField($"Shiny Stats Channel", guildConfig.ShinyStats?.ChannelId == 0 ? "Not Set" : $"{ctx.Guild.GetChannel(guildConfig.ShinyStats.ChannelId)?.Name}:{guildConfig.ShinyStats?.ChannelId}", true);
            eb.AddField($"Clear Previous Shiny Stats", guildConfig.ShinyStats?.ClearMessages ?? false ? "Yes" : "No", true);
            eb.AddField($"Icon Style", guildConfig.IconStyle, true);
            await ctx.RespondAsync(embed: eb);
        }
    }
}
//List/add/remove quest channel pruning
//Manage shiny stats
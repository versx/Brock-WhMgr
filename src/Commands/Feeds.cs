namespace WhMgr.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;

    using WhMgr.Configuration;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Localization;

    public class Feeds : BaseCommandModule
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("FEEDS", Program.LogLevel);

        private readonly WhConfigHolder _config;

        public Feeds(WhConfigHolder config)
        {
            _config = config;
        }

        [
            Command("feeds"),
            Aliases("cities", "roles"),
            Description("Shows a list of assignable city roles and other roles.")
        ]
        public async Task FeedsAsync(CommandContext ctx)
        {
            if (!await ctx.IsDirectMessageSupported(_config.Instance))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));
            if (!_config.Instance.Servers.ContainsKey(guildId))
                return;

            var server = _config.Instance.Servers[guildId];
            var cityRoles = server.Geofences.Select(x => x.Name).ToList();
            cityRoles.Sort();
            var sb = new StringBuilder();
            sb.AppendLine(Translator.Instance.Translate("FEEDS_AVAILABLE_CITY_ROLES"));
            sb.AppendLine($"- {string.Join($"{Environment.NewLine}- ", cityRoles)}");
            sb.AppendLine();
            sb.AppendLine($"- {Strings.All}");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(Translator.Instance.Translate("FEEDS_TYPE_COMMAND_ASSIGN_ROLE").FormatText(server.CommandPrefix));
            var eb = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = sb.ToString(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{ctx.Guild?.Name ?? Strings.Creator} | {DateTime.Now}",
                    IconUrl = ctx.Guild?.IconUrl
                }
            };

            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync(embed: eb.Build());
        }

        [
            Command("feedme"),
            Description("Joins a city feed.\r\n\r\n**Example:** `.feedme City1,City2`")
        ]
        public async Task FeedMeAsync(CommandContext ctx,
            [Description("City name to join or all."), RemainingText] string cityName = null)
        {
            if (!await ctx.IsDirectMessageSupported(_config.Instance))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));
            if (!_config.Instance.Servers.ContainsKey(guildId))
                return;

            var server = _config.Instance.Servers[guildId];
            var isSupporter = await ctx.Client.IsSupporterOrHigher(ctx.User.Id, guildId, _config.Instance);
            var isFreeRole = !string.IsNullOrEmpty(server.FreeRoleName) && string.Compare(cityName, server.FreeRoleName, true) == 0;
            if (server.CitiesRequireSupporterRole && !isSupporter && !isFreeRole)
            {
                await ctx.DonateUnlockFeaturesMessage();
                return;
            }

            if (string.Compare(cityName, Strings.All, true) == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_PLEASE_WAIT", ctx.User.Username), DiscordColor.Green);
                await AssignAllDefaultFeedRoles(ctx);
                return;
            }

            var assigned = new List<string>();
            var alreadyAssigned = new List<string>();

            try
            {
                var cityNames = cityName.RemoveSpaces();
                var cityRoles = server.Geofences.Select(x => x.Name.ToLower());
                foreach (var city in cityNames)
                {
                    if (!isFreeRole && !cityRoles.Contains(city.ToLower()))
                    {
                        await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_INVALID_CITY_NAME_TYPE_COMMAND").FormatText(ctx.User.Username, city, server.CommandPrefix), DiscordColor.Red);
                        continue;
                    }

                    var cityRole = ctx.Guild.GetRoleFromName(city);
                    if (cityRole == null)
                    {
                        await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_INVALID_CITY_NAME").FormatText(ctx.User.Username, city), DiscordColor.Red);
                        continue;
                    }

                    var result = await AddFeedRole(ctx.Member, cityRole);
                    if (result)
                    {
                        assigned.Add(cityRole.Name);
                    }
                    else
                    {
                        alreadyAssigned.Add(cityRole.Name);
                    }

                    Thread.Sleep(200);
                }

                if (assigned.Count == 0 && alreadyAssigned.Count == 0)
                {
                    // TODO: Log
                    //ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Debug, "Feeds", $"No roles assigned or already assigned for user {ctx.User.Username} ({ctx.User.Id}). Value: {string.Join(", ", cityNames)}", DateTime.Now);
                    return;
                }

                await ctx.RespondEmbed
                (
                    (assigned.Count > 0
                        ? Translator.Instance.Translate("FEEDS_ASSIGNED_ROLES").FormatText(ctx.User.Username, string.Join("**, **", assigned))
                        : string.Empty) +
                    (alreadyAssigned.Count > 0
                        ? Translator.Instance.Translate("FEEDS_UNASSIGNED_ROLES").FormatText(ctx.User.Username, string.Join("**, **", alreadyAssigned))
                        : string.Empty)
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        [
            Command("feedmenot"),
            Description("Leaves a city's feed.")
        ]
        public async Task FeedMeNotAsync(CommandContext ctx,
            [Description("City name to leave or all."), RemainingText] string cityName)
        {
            if (!await ctx.IsDirectMessageSupported(_config.Instance))
                return;

            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));
            if (!_config.Instance.Servers.ContainsKey(guildId))
                return;

            var server = _config.Instance.Servers[guildId];
            var isSupporter = await ctx.Client.IsSupporterOrHigher(ctx.User.Id, guildId, _config.Instance);
            var isFreeRole = !string.IsNullOrEmpty(server.FreeRoleName) && string.Compare(cityName, server.FreeRoleName, true) == 0;
            if (server.CitiesRequireSupporterRole && !isSupporter && !isFreeRole)
            {
                await ctx.DonateUnlockFeaturesMessage();
                return;
            }

            if (string.Compare(cityName, Strings.All, true) == 0)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_PLEASE_WAIT", ctx.User.Username), DiscordColor.Green);
                await RemoveAllDefaultFeedRoles(ctx);
                return;
            }

            var unassigned = new List<string>();
            var alreadyUnassigned = new List<string>();

            try
            {
                var cityNames = cityName.RemoveSpaces();
                var areas = server.Geofences.Select(x => x.Name).ToList();
                foreach (var city in cityNames)
                {
                    if (!isFreeRole && !areas.Exists(x => string.Compare(city, x, true) == 0))
                    {
                        await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_INVALID_CITY_NAME_TYPE_COMMAND").FormatText(ctx.User.Username, city, server.CommandPrefix), DiscordColor.Red);
                        continue;
                    }

                    var cityRole = ctx.Guild.GetRoleFromName(city);
                    if (cityRole == null)
                    {
                        await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_INVALID_CITY_NAME").FormatText(ctx.User.Username, city), DiscordColor.Red);
                        continue;
                    }

                    if (await RemoveFeedRole(ctx.Member, cityRole))
                    {
                        unassigned.Add(cityRole.Name);
                    }
                    else
                    {
                        alreadyUnassigned.Add(cityRole.Name);
                    }

                    Thread.Sleep(200);
                }

                await ctx.RespondEmbed
                (
                    (unassigned.Count > 0
                        ? Translator.Instance.Translate("FEEDS_UNASSIGNED_ROLES").FormatText(ctx.User.Username, string.Join("**, **", unassigned))
                        : string.Empty) +
                    (alreadyUnassigned.Count > 0
                        ? Translator.Instance.Translate("FEEDS_UNASSIGNED_ROLES_ALREADY").FormatText(ctx.User.Username, string.Join("**, **", alreadyUnassigned))
                        : string.Empty)
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async Task AssignAllDefaultFeedRoles(CommandContext ctx)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));

            if (_config.Instance.Servers[guildId].Geofences == null)
            {
                _logger.Warn($"City roles empty.");
                return;
            }

            try
            {
                var server = _config.Instance.Servers[guildId];
                var areas = server.Geofences.Select(x => x.Name).ToList();
                for (var i = 0; i < areas.Count; i++)
                {
                    var city = areas[i];
                    var cityRole = ctx.Guild.GetRoleFromName(city);
                    if (cityRole == null)
                    {
                        _logger.Error($"Failed to get city raid role from city {city}.");
                        continue;
                    }

                    var result = await AddFeedRole(ctx.Member, cityRole);
                    if (!result)
                    {
                        _logger.Error($"Failed to assign role {cityRole.Name} to user {ctx.User.Username} ({ctx.User.Id}).");
                    }

                    Thread.Sleep(500);
                }
            }
            catch (Exception)
            {
                _logger.Error($"Failed to add feed role, make sure bot has correct permissions.");
                await ctx.RespondEmbed($"Failed to add feed role, make sure bot has correct permissions.");
                return;
            }

            await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_ASSIGNED_ALL_ROLES").FormatText(ctx.User.Username));
        }

        private async Task RemoveAllDefaultFeedRoles(CommandContext ctx)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));

            if (_config.Instance.Servers[guildId].Geofences == null)
            {
                _logger.Warn($"City roles empty.");
                return;
            }

            try
            {
                var server = _config.Instance.Servers[guildId];
                var areas = server.Geofences.Select(x => x.Name).ToList();
                for (var i = 0; i < areas.Count; i++)
                {
                    var city = areas[i];
                    var cityRole = ctx.Guild.GetRoleFromName(city);
                    if (cityRole == null)
                    {
                        _logger.Error($"Failed to get city role from city {city}.");
                        continue;
                    }

                    var result = await RemoveFeedRole(ctx.Member, cityRole);
                    if (!result)
                    {
                        _logger.Error($"Failed to remove role {cityRole.Name} from user {ctx.User.Username} ({ctx.User.Id}).");
                    }

                    Thread.Sleep(200);
                }
            }
            catch (Exception)
            {
                _logger.Error($"Failed to remove feed role, make sure bot has correct permissions.");
                await ctx.RespondEmbed($"Failed to remove feed role, make sure bot has correct permissions.");
                return;
            }

            await ctx.RespondEmbed(Translator.Instance.Translate("FEEDS_UNASSIGNED_ALL_ROLES").FormatText(ctx.User.Username));
        }

        private async Task<bool> AddFeedRole(DiscordMember member, DiscordRole city)
        {
            if (city == null)
            {
                _logger.Error($"Failed to find city role {city?.Name}, please make sure it exists.");
                return false;
            }

            await member.GrantRoleAsync(city, "City role role assignment.");
            return true;
        }

        private async Task<bool> RemoveFeedRole(DiscordMember member, DiscordRole city)
        {
            if (city == null)
            {
                _logger.Error($"Failed to find city role {city?.Name}, please make sure it exists.");
                return false;
            }

            await member.RevokeRoleAsync(city, "City role removal.");
            return true;
        }
    }
}
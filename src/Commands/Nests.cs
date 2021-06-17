namespace WhMgr.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using ServiceStack;
    using ServiceStack.OrmLite;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Configuration;
    using WhMgr.Data;
    using WhMgr.Data.Models;
    using WhMgr.Diagnostics;
    using WhMgr.Extensions;
    using WhMgr.Geofence;
    using WhMgr.Localization;
    using WhMgr.Net.Webhooks;
    using WhMgr.Osm;
    using WhMgr.Services;
    using WhMgr.Utilities;

    public class Nests : BaseCommandModule
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("NESTS", Program.LogLevel);

        private readonly WhConfigHolder _config;
        private readonly WebhookController _whm;
        private readonly OsmManager _osmManager;

        public Nests(WhConfigHolder config, WebhookController whm, OsmManager osm)
        {
            _config = config;
            _whm = whm;
            _osmManager = osm;
        }

        [
            Command("nests"),
            Description(""),
            RequirePermissions(Permissions.KickMembers)
        ]
        public async Task PostNestsAsync(CommandContext ctx,
            [Description("")] string args = null)
        {
            var guildId = ctx.Guild?.Id ?? ctx.Client.Guilds.Keys.FirstOrDefault(x => _config.Instance.Servers.ContainsKey(x));
            if (!_config.Instance.Servers.ContainsKey(guildId))
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NOT_IN_DISCORD_SERVER"), DiscordColor.Red);
                return;
            }

            var server = _config.Instance.Servers[guildId];
            var channelId = server.NestsChannelId;
            var channel = await ctx.Client.GetChannelAsync(channelId);
            if (channel == null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NESTS_DISABLED").FormatText(ctx.User.Username), DiscordColor.Red);
                return;
            }

            var deleted = await ctx.Client.DeleteMessages(channelId);
            if (deleted.Item2 == 0)
            {
                _logger.Warn($"Failed to delete messages in channel: {channelId}");
            }

            var nests = GetNests(_config.Instance.Database.Nests.ToString());
            if (nests == null)
            {
                await ctx.RespondEmbed(Translator.Instance.Translate("ERROR_NESTS_LIST").FormatText(ctx.User.Username));
                return;
            }

            var postNestAsList = string.Compare(args, "list", true) == 0;
            if (postNestAsList)
            {
                var groupedNests = GroupNests(guildId, nests);
                groupedNests.ToImmutableSortedDictionary();
                var sortedKeys = groupedNests.Keys.ToList();
                sortedKeys.Sort();
                foreach (var key in sortedKeys)
                {
                    var eb = new DiscordEmbedBuilder
                    {
                        Title = key,
                        Description = string.Empty,
                        Color = DiscordColor.Green
                    };
                    var message = string.Empty;
                    foreach (var nest in groupedNests[key])
                    {
                        if (nest.Average < server.NestsMinimumPerHour)
                            continue;

                        var pkmn = MasterFile.GetPokemon(nest.PokemonId, 0);
                        var pkmnName = Translator.Instance.GetPokemonName(pkmn.PokedexId);
                        var gmapsLink = string.Format(Strings.GoogleMaps, nest.Latitude, nest.Longitude);
                        // TODO: Check if possible shiny
                        message += $"[**{nest.Name}**]({gmapsLink}): {pkmnName} (#{nest.PokemonId}) {nest.Average:N0} per hour\r\n";
                        if (message.Length >= 2048)
                        {
                            eb.Description = message.Substring(0, Math.Min(message.Length, 2048));
                            message = string.Empty;
                            await channel.SendMessageAsync(embed: eb);
                            eb = new DiscordEmbedBuilder
                            {
                                Title = key,
                                Description = string.Empty,
                                Color = DiscordColor.Green
                            };
                        }
                    }
                    if (message.Length > 0)
                    {
                        eb.Description = message;
                        message = string.Empty;
                        await channel.SendMessageAsync(embed: eb);
                    }
                    Thread.Sleep(1000);
                }
            }
            else
            {
                var cities = server.Geofences.Select(x => x.Name.ToLower()).ToList();
                for (var i = 0; i < nests.Count; i++)
                {
                    var nest = nests[i];
                    if (nest.Average == 0)
                        continue;

                    try
                    {
                        var eb = GenerateNestMessage(guildId, ctx.Client, nest);
                        var geofence = _whm.GetGeofence(guildId, nest.Latitude, nest.Longitude);
                        if (geofence == null)
                        {
                            //_logger.Warn($"Failed to find geofence for nest {nest.Key}.");
                            continue;
                        }

                        if (!cities.Contains(geofence.Name.ToLower()))
                            continue;

                        if (nest.Average < server.NestsMinimumPerHour)
                            continue;

                        await channel.SendMessageAsync(embed: eb);
                        Thread.Sleep(200);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                }
            }
        }

        public DiscordEmbed GenerateNestMessage(ulong guildId, DiscordClient client, Nest nest)
        {
            var alertMessageType = AlertMessageType.Nests;
            var alertMessage = /*alarm?.Alerts[alertMessageType] ??*/ AlertMessage.Defaults[alertMessageType]; // TODO: Add nestAlert config option
            var server = _config.Instance.Servers[guildId];
            var pokemonImageUrl = IconFetcher.Instance.GetPokemonIcon(server.IconStyle, nest.PokemonId);
            var properties = GetProperties(client.Guilds[guildId], nest, pokemonImageUrl);
            var eb = new DiscordEmbedBuilder
            {
                Title = DynamicReplacementEngine.ReplaceText(alertMessage.Title, properties),
                Url = DynamicReplacementEngine.ReplaceText(alertMessage.Url, properties),
                ImageUrl = DynamicReplacementEngine.ReplaceText(alertMessage.ImageUrl, properties),
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = DynamicReplacementEngine.ReplaceText(alertMessage.IconUrl, properties),
                },
                Description = DynamicReplacementEngine.ReplaceText(alertMessage.Content, properties),
                Color = DiscordColor.Green,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = DynamicReplacementEngine.ReplaceText(alertMessage.Footer?.Text, properties),
                    IconUrl = DynamicReplacementEngine.ReplaceText(alertMessage.Footer?.IconUrl, properties)
                }
            };
            return eb.Build();
        }

        public IReadOnlyDictionary<string, string> GetProperties(DiscordGuild guild, Nest nest, string pokemonImageUrl)
        {
            var pkmnInfo = MasterFile.GetPokemon(nest.PokemonId, 0);
            var pkmnImage = pokemonImageUrl;
            var nestName = nest.Name ?? "Unknown";
            var type1 = pkmnInfo?.Types?[0];
            var type2 = pkmnInfo?.Types?.Count > 1 ? pkmnInfo.Types?[1] : PokemonType.None;
            var type1Emoji = pkmnInfo?.Types?[0].GetTypeEmojiIcons();
            var type2Emoji = pkmnInfo?.Types?.Count > 1 ? pkmnInfo?.Types?[1].GetTypeEmojiIcons() : string.Empty;
            var typeEmojis = $"{type1Emoji} {type2Emoji}";
            var gmapsLink = string.Format(Strings.GoogleMaps, nest.Latitude, nest.Longitude);
            var appleMapsLink = string.Format(Strings.AppleMaps, nest.Latitude, nest.Longitude);
            var wazeMapsLink = string.Format(Strings.WazeMaps, nest.Latitude, nest.Longitude);
            var scannerMapsLink = string.Format(_config.Instance.Urls.ScannerMap, nest.Latitude, nest.Longitude);
            var staticMapLink = StaticMap.GetUrl(_config.Instance.Urls.StaticMap, _config.Instance.StaticMaps["nests"], nest.Latitude, nest.Longitude, pkmnImage, Net.Models.PokemonTeam.All, _osmManager.GetNest(nest.Name)?.FirstOrDefault());
            var geofence = _whm.GetGeofence(guild.Id, nest.Latitude, nest.Longitude);
            var city = geofence?.Name ?? "Unknown";
            var address = new Location(null, city, nest.Latitude, nest.Longitude).GetAddress(_config.Instance);

            var dict = new Dictionary<string, string>
            {
                //Main properties
                { "pkmn_id", Convert.ToString(nest.PokemonId) },
                { "pkmn_id_3", nest.PokemonId.ToString("D3") },
                { "pkmn_name", pkmnInfo?.Name },
                { "pkmn_img_url", pkmnImage },
                { "avg_spawns", Convert.ToString(nest.Average) },
                { "nest_name", nestName },
                { "type_1", Convert.ToString(type1) },
                { "type_2", Convert.ToString(type2) },
                { "type_1_emoji", type1Emoji },
                { "type_2_emoji", type2Emoji },
                { "types", $"{type1} | {type2}" },
                { "types_emojis", typeEmojis },

                //Location properties
                { "geofence", city },
                { "lat", Convert.ToString(nest.Latitude) },
                { "lng", Convert.ToString(nest.Longitude) },
                { "lat_5", Convert.ToString(Math.Round(nest.Latitude, 5)) },
                { "lng_5", Convert.ToString(Math.Round(nest.Longitude, 5)) },

                //Location links
                { "tilemaps_url", staticMapLink },
                { "gmaps_url", gmapsLink },
                { "applemaps_url", appleMapsLink },
                { "wazemaps_url", wazeMapsLink },
                { "scanmaps_url", scannerMapsLink },

                { "address", address?.Address },

                // Discord Guild properties
                { "guild_name", guild?.Name },
                { "guild_img_url", guild?.IconUrl },

                { "date_time", DateTime.Now.ToString() },

                //Misc properties
                { "br", "\r\n" }
            };
            return dict;
        }

        private Dictionary<string, List<Nest>> GroupNests(ulong guildId, IEnumerable<Nest> nests)
        {
            var dict = new Dictionary<string, List<Nest>>();
            foreach (var nest in nests)
            {
                var geofence = _whm.GetGeofence(guildId, nest.Latitude, nest.Longitude);
                if (geofence == null)
                {
                    _logger.Warn($"Failed to find geofence for nest {nest.Name}.");
                    continue;
                }
                var geofenceName = geofence.Name;
                var server = _config.Instance.Servers[guildId];
                var cities = server.Geofences.Select(x => x.Name.ToLower()).ToList();
                if (!cities.Contains(geofenceName.ToLower()))
                    continue;

                if (dict.ContainsKey(geofenceName))
                {
                    dict[geofenceName].Add(nest);
                }
                else
                {
                    dict.Add(geofenceName, new List<Nest> { nest });
                }
                dict[geofenceName].Sort((x, y) => x.Name.CompareTo(y.Name));
            }
            return dict;
        }

        /// <summary>
        /// Get a list of nests from the database
        /// </summary>
        /// <param name="nestsConnectionString"></param>
        /// <returns></returns>
        public static List<Nest> GetNests(string nestsConnectionString = null)
        {
            if (string.IsNullOrEmpty(nestsConnectionString))
                return null;

            try
            {
                using (var db = DataAccessLayer.CreateFactory(nestsConnectionString).Open())
                {
                    var nests = db.LoadSelect<Nest>();
                    return nests;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return null;
        }
    }
}
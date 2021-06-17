namespace WhMgr.Net.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;
    using ServiceStack.DataAnnotations;
    using ServiceStack.OrmLite;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Alarms.Models;
    using WhMgr.Configuration;
    using WhMgr.Data;
    using WhMgr.Diagnostics;
    using WhMgr.Geofence;
    using WhMgr.Services;
    using WhMgr.Utilities;

    /// <summary>
    /// RealDeviceMap Gym Details webhook model class.
    /// </summary>
    [Alias("gym")]
    public sealed class GymDetailsData
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("GYMDETAILSDATA", Program.LogLevel);

        public const string WebhookHeader = "gym_details";

        #region Properties

        [
            JsonProperty("id"),
            Alias("id")
        ]
        public string GymId { get; set; }

        [
            JsonProperty("name"),
            Alias("name")
        ]
        public string GymName { get; set; } = "Unknown";

        [
            JsonProperty("url"),
            Alias("name")
        ]
        public string Url { get; set; }

        [
            JsonProperty("latitude"),
            Alias("lat")
        ]
        public double Latitude { get; set; }

        [
            JsonProperty("longitude"),
            Alias("lon")
        ]
        public double Longitude { get; set; }

        [
            JsonProperty("team"),
            Alias("team_id")
        ]
        public PokemonTeam Team { get; set; } = PokemonTeam.Neutral;

        [
            JsonProperty("slots_available"),
            Alias("availble_slots") // TODO: Typflo
        ]
        public ushort SlotsAvailable { get; set; }

        [
            JsonProperty("sponsor_id"),
            Alias("sponsor_id")
        ]
        public bool SponsorId { get; set; }

        [
            JsonProperty("in_battle"),
            Alias("in_battle")
        ]
        public bool InBattle { get; set; }

        #endregion

        public DiscordEmbedNotification GenerateGymMessage(ulong guildId, DiscordClient client, WhConfig whConfig, AlarmObject alarm, GymDetailsData oldGym, string city)
        {
            var server = whConfig.Servers[guildId];
            var alertType = AlertMessageType.Gyms;
            var alert = alarm?.Alerts[alertType] ?? server.DmAlerts?[alertType] ?? AlertMessage.Defaults[alertType];
            var properties = GetProperties(client.Guilds[guildId], whConfig, city, oldGym);
            var eb = new DiscordEmbedBuilder
            {
                Title = DynamicReplacementEngine.ReplaceText(alert.Title, properties),
                Url = DynamicReplacementEngine.ReplaceText(alert.Url, properties),
                ImageUrl = DynamicReplacementEngine.ReplaceText(alert.ImageUrl, properties),
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = DynamicReplacementEngine.ReplaceText(alert.IconUrl, properties),
                },
                Description = DynamicReplacementEngine.ReplaceText(alert.Content, properties),
                Color = Team == PokemonTeam.Mystic ? DiscordColor.Blue :
                        Team == PokemonTeam.Valor ? DiscordColor.Red :
                        Team == PokemonTeam.Instinct ? DiscordColor.Yellow :
                        DiscordColor.LightGray,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = DynamicReplacementEngine.ReplaceText(alert.Footer?.Text, properties),
                    IconUrl = DynamicReplacementEngine.ReplaceText(alert.Footer?.IconUrl, properties)
                }
            };
            var username = DynamicReplacementEngine.ReplaceText(alert.Username, properties);
            var iconUrl = DynamicReplacementEngine.ReplaceText(alert.AvatarUrl, properties);
            var description = DynamicReplacementEngine.ReplaceText(alarm?.Description, properties);
            return new DiscordEmbedNotification(username, iconUrl, description, new List<DiscordEmbed> { eb.Build() });
        }

        private IReadOnlyDictionary<string, string> GetProperties(DiscordGuild guild, WhConfig whConfig, string city, GymDetailsData oldGym)
        {
            var exEmojiId = MasterFile.Instance.Emojis["ex"];
            var exEmoji = string.IsNullOrEmpty(MasterFile.Instance.CustomEmojis["ex"]) ? exEmojiId > 0
                ? string.Format(Strings.EmojiSchema, "ex", exEmojiId): "EX"
                : MasterFile.Instance.CustomEmojis["ex"];
            var teamEmojiId = MasterFile.Instance.Emojis[Team.ToString().ToLower()];
            var teamEmoji = string.IsNullOrEmpty(MasterFile.Instance.CustomEmojis[Team.ToString().ToLower()])
                ? teamEmojiId > 0
                    ? string.Format(Strings.EmojiSchema, Team.ToString().ToLower(), teamEmojiId)
                    : Team.ToString()
                : MasterFile.Instance.CustomEmojis[Team.ToString().ToLower()];
            var oldTeamEmojiId = MasterFile.Instance.Emojis[oldGym?.Team.ToString().ToLower()];
            var oldTeamEmoji = string.IsNullOrEmpty(MasterFile.Instance.CustomEmojis[oldGym?.Team.ToString().ToLower()])
                ? oldTeamEmojiId > 0
                    ? string.Format(Strings.EmojiSchema, oldGym?.Team.ToString().ToLower(), oldTeamEmojiId)
                    : oldGym?.Team.ToString()
                : MasterFile.Instance.CustomEmojis[oldGym.Team.ToString().ToLower()];

            var gmapsLink = string.Format(Strings.GoogleMaps, Latitude, Longitude);
            var appleMapsLink = string.Format(Strings.AppleMaps, Latitude, Longitude);
            var wazeMapsLink = string.Format(Strings.WazeMaps, Latitude, Longitude);
            var scannerMapsLink = string.Format(whConfig.Urls.ScannerMap, Latitude, Longitude);
            var gymImageUrl = $"https://raw.githubusercontent.com/nileplumb/PkmnHomeIcons/ICONS/ICONS/gym/{Convert.ToInt32(Team)}.png"; // TODO: Build gym image url
            var staticMapLink = StaticMap.GetUrl(whConfig.Urls.StaticMap, whConfig.StaticMaps["gyms"], Latitude, Longitude, gymImageUrl);
            //var staticMapLink = string.Format(whConfig.Urls.StaticMap, Latitude, Longitude);//whConfig.Urls.StaticMap.Gyms.Enabled ? string.Format(whConfig.Urls.StaticMap.Gyms.Url, Latitude, Longitude) : string.Empty
            var gmapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, gmapsLink);
            var appleMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, appleMapsLink);
            var wazeMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, wazeMapsLink);
            var scannerMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, scannerMapsLink);
            var address = new Location(null, city, Latitude, Longitude).GetAddress(whConfig);
            //var staticMapLocationLink = string.IsNullOrEmpty(whConfig.ShortUrlApiUrl) ? staticMapLink : NetUtil.CreateShortUrl(whConfig.ShortUrlApiUrl, staticMapLink);

            const string defaultMissingValue = "?";
            var dict = new Dictionary<string, string>
            {
                //Main properties
                { "gym_id", GymId },
                { "gym_name", GymName },
                { "gym_url", Url },
                { "gym_team", Team.ToString() },
                { "gym_team_id", Convert.ToInt32(Team).ToString() },
                { "gym_team_emoji", teamEmoji },
                { "old_gym_team", oldGym.Team.ToString() },
                { "old_gym_team_id", Convert.ToInt32(oldGym.Team).ToString() },
                { "old_gym_team_emoji", oldTeamEmoji },
                { "team_changed", Convert.ToString(oldGym?.Team != Team) },
                { "in_battle", Convert.ToString(InBattle) },
                { "under_attack", Convert.ToString(InBattle) },
                { "is_ex", Convert.ToString(SponsorId) },
                { "ex_emoji", exEmoji },
                { "slots_available", SlotsAvailable == 0
                                        ? "Full"
                                        : SlotsAvailable == 6
                                            ? "Empty"
                                            : SlotsAvailable.ToString("N0") },

                //Location properties
                { "geofence", city ?? defaultMissingValue },
                { "lat", Latitude.ToString() },
                { "lng", Longitude.ToString() },
                { "lat_5", Latitude.ToString("0.00000") },
                { "lng_5", Longitude.ToString("0.00000") },

                //Location links
                { "tilemaps_url", staticMapLink },
                { "gmaps_url", gmapsLocationLink },
                { "applemaps_url", appleMapsLocationLink },
                { "wazemaps_url", wazeMapsLocationLink },
                { "scanmaps_url", scannerMapsLocationLink },

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

        internal static Dictionary<string, GymDetailsData> GetGyms(string connectionString = "")
        {
            if (string.IsNullOrEmpty(connectionString))
                return null;

            try
            {
                using (var db = DataAccessLayer.CreateFactory(connectionString).Open())
                {
                    var gyms = db.LoadSelect<GymDetailsData>();
                    var dict = gyms?.ToDictionary(x => x.GymId, x => x);
                    return dict;
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
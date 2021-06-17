namespace WhMgr.Net.Models
{
    using System;
    using System.Collections.Generic;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Alarms.Models;
    using WhMgr.Commands;
    using WhMgr.Configuration;
    using WhMgr.Data;
    using WhMgr.Data.Models;
    using WhMgr.Extensions;
    using WhMgr.Geofence;
    using WhMgr.Localization;
    using WhMgr.Services;
    using WhMgr.Utilities;

    /// <summary>
    /// RealDeviceMap Pokestop (lure/invasion) webhook model class.
    /// </summary>
    public sealed class PokestopData
    {
        public const string WebhookHeader = "pokestop";
        public const string WebhookHeaderInvasion = "invasion";

        #region Properties

        [JsonProperty("pokestop_id")]
        public string PokestopId { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = "Unknown";

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("lure_expiration")]
        public long LureExpire { get; set; }

        [JsonIgnore]
        public DateTime LureExpireTime { get; set; }

        [JsonProperty("lure_id")]
        public PokestopLureType LureType { get; set; }

        [JsonProperty("incident_expire_timestamp")]
        public long IncidentExpire { get; set; }

        [JsonIgnore]
        public DateTime InvasionExpireTime { get; set; }

        [JsonProperty("grunt_type")]
        public InvasionCharacter GruntType { get; set; }

        [JsonProperty("last_modified")]
        public ulong LastModified { get; set; }

        [JsonProperty("updated")]
        public ulong Updated { get; set; }

        [JsonIgnore]
        public bool HasLure => LureExpire > 0 && LureType != PokestopLureType.None && LureExpireTime > DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);

        [JsonIgnore]
        public bool HasInvasion => IncidentExpire > 0 && InvasionExpireTime > DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiate a new <see cref="PokestopData"/> class.
        /// </summary>
        public PokestopData()
        {
            SetTimes();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set expire times because .NET doesn't support Unix timestamp deserialization to <seealso cref="DateTime"/> class by default.
        /// </summary>
        public void SetTimes()
        {
            LureExpireTime = LureExpire
                .FromUnix()
                .ConvertTimeFromCoordinates(Latitude, Longitude);

            InvasionExpireTime = IncidentExpire
                .FromUnix()
                .ConvertTimeFromCoordinates(Latitude, Longitude);
        }

        public DiscordEmbedNotification GeneratePokestopMessage(ulong guildId, DiscordClient client, WhConfig whConfig, AlarmObject alarm, string city, bool useLure, bool useInvasion)
        {
            var server = whConfig.Servers[guildId];
            var alertType = useInvasion ? AlertMessageType.Invasions : useLure ? AlertMessageType.Lures : AlertMessageType.Pokestops;
            var alert = alarm?.Alerts[alertType] ?? server.DmAlerts?[alertType] ?? AlertMessage.Defaults[alertType];
            var properties = GetProperties(client.Guilds[guildId], whConfig, city, useLure, useInvasion);
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
                Color = useInvasion
                    ? new DiscordColor(server.DiscordEmbedColors.Pokestops.Invasions)
                    : useLure
                        ? LureType.BuildLureColor(server)
                        : DiscordColor.CornflowerBlue,
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

        #endregion

        #region Private Methods

        private IReadOnlyDictionary<string, string> GetProperties(DiscordGuild guild, WhConfig whConfig, string city, bool useLure, bool useInvasion)
        {
            var lureImageUrl = IconFetcher.Instance.GetLureIcon(whConfig.Servers[guild.Id].IconStyle, LureType);
            var invasionImageUrl = IconFetcher.Instance.GetInvasionIcon(whConfig.Servers[guild.Id].IconStyle, GruntType);
            var imageUrl = useInvasion ? invasionImageUrl : useLure ? lureImageUrl : Url;
            var gmapsLink = string.Format(Strings.GoogleMaps, Latitude, Longitude);
            var appleMapsLink = string.Format(Strings.AppleMaps, Latitude, Longitude);
            var wazeMapsLink = string.Format(Strings.WazeMaps, Latitude, Longitude);
            var scannerMapsLink = string.Format(whConfig.Urls.ScannerMap, Latitude, Longitude);
            var staticMapLink = StaticMap.GetUrl(whConfig.Urls.StaticMap, useInvasion ? whConfig.StaticMaps["invasions"] : useLure ? whConfig.StaticMaps["lures"] : /* TODO: */"", Latitude, Longitude, imageUrl);
            var gmapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, gmapsLink);
            var appleMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, appleMapsLink);
            var wazeMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, wazeMapsLink);
            var scannerMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, scannerMapsLink);
            var address = new Location(null, city, Latitude, Longitude).GetAddress(whConfig);
            //var staticMapLocationLink = string.IsNullOrEmpty(whConfig.ShortUrlApiUrl) ? staticMapLink : NetUtil.CreateShortUrl(whConfig.ShortUrlApiUrl, staticMapLink);
            var invasion = MasterFile.Instance.GruntTypes.ContainsKey(GruntType) ? MasterFile.Instance.GruntTypes[GruntType] : null;
            var leaderString = Translator.Instance.Translate("grunt_" + Convert.ToInt32(GruntType));
            var pokemonType = MasterFile.Instance.GruntTypes.ContainsKey(GruntType) ? GetPokemonTypeFromString(invasion?.Type) : PokemonType.None;
            var invasionTypeEmoji = pokemonType == PokemonType.None
                ? leaderString
                : pokemonType.GetTypeEmojiIcons();
            var invasionEncounters = GruntType > 0 ? invasion.GetPossibleInvasionEncounters() : string.Empty;

            var now = DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);
            var lureExpireTimeLeft = now.GetTimeRemaining(LureExpireTime).ToReadableStringNoSeconds();
            var invasionExpireTimeLeft = now.GetTimeRemaining(InvasionExpireTime).ToReadableStringNoSeconds();

            const string defaultMissingValue = "?";
            var dict = new Dictionary<string, string>
            {
                //Main properties
                { "has_lure", Convert.ToString(HasLure) },
                { "lure_type", LureType.ToString() },
                { "lure_expire_time", LureExpireTime.ToLongTimeString() },
                { "lure_expire_time_24h", LureExpireTime.ToString("HH:mm:ss") },
                { "lure_expire_time_left", lureExpireTimeLeft },
                { "has_invasion", Convert.ToString(HasInvasion) },
                { "grunt_type", invasion?.Type },
                { "grunt_type_emoji", invasionTypeEmoji },
                { "grunt_gender", invasion?.Grunt },
                { "invasion_expire_time", InvasionExpireTime.ToLongTimeString() },
                { "invasion_expire_time_24h", InvasionExpireTime.ToString("HH:mm:ss") },
                { "invasion_expire_time_left", invasionExpireTimeLeft },
                { "invasion_encounters", $"**Encounter Reward Chance:**\r\n" + invasionEncounters },

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

                //Pokestop properties
                { "pokestop_id", PokestopId ?? defaultMissingValue },
                { "pokestop_name", Name ?? defaultMissingValue },
                { "pokestop_url", Url ?? defaultMissingValue },
                { "lure_img_url", lureImageUrl },
                { "invasion_img_url", invasionImageUrl },

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

        #endregion

        public static PokemonType GetPokemonTypeFromString(string pokemonType)
        {
            var type = pokemonType.ToLower();
            if (type.Contains("bug"))
                return PokemonType.Bug;
            else if (type.Contains("dark"))
                return PokemonType.Dark;
            else if (type.Contains("dragon"))
                return PokemonType.Dragon;
            else if (type.Contains("electric"))
                return PokemonType.Electric;
            else if (type.Contains("fairy"))
                return PokemonType.Fairy;
            else if (type.Contains("fighting") || type.Contains("fight"))
                return PokemonType.Fighting;
            else if (type.Contains("fire"))
                return PokemonType.Fire;
            else if (type.Contains("flying") || type.Contains("fly"))
                return PokemonType.Flying;
            else if (type.Contains("ghost"))
                return PokemonType.Ghost;
            else if (type.Contains("grass"))
                return PokemonType.Grass;
            else if (type.Contains("ground"))
                return PokemonType.Ground;
            else if (type.Contains("ice"))
                return PokemonType.Ice;
            //else if (type.Contains("tierii") || type.Contains("none") || type.Contains("tier2") || type.Contains("t2"))
            //    return PokemonType.None;
            else if (type.Contains("normal"))
                return PokemonType.Normal;
            else if (type.Contains("poison"))
                return PokemonType.Poison;
            else if (type.Contains("psychic"))
                return PokemonType.Psychic;
            else if (type.Contains("rock"))
                return PokemonType.Rock;
            else if (type.Contains("steel"))
                return PokemonType.Steel;
            else if (type.Contains("water"))
                return PokemonType.Water;
            else
                return PokemonType.None;
        }
    }
}
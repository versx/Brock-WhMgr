﻿namespace WhMgr.Services.Webhook.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using DSharpPlus.Entities;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;

    using WhMgr.Data;
    using WhMgr.Extensions;
    using WhMgr.Localization;
    using WhMgr.Services.Alarms;
    using WhMgr.Services.Alarms.Embeds;
    using WhMgr.Services.Discord.Models;
    using WhMgr.Utilities;

    /// <summary>
    /// RealDeviceMap Pokestop (lure/invasion) webhook model class.
    /// </summary>
    public sealed class PokestopData : IWebhookData
    {
        #region Properties

        [JsonPropertyName("pokestop_id")]
        public string PokestopId { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unknown";

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("lure_expiration")]
        public long LureExpire { get; set; }

        [JsonIgnore]
        public DateTime LureExpireTime { get; set; }

        [JsonPropertyName("lure_id")]
        public PokestopLureType LureType { get; set; }

        [JsonPropertyName("incident_expire_timestamp")]
        public long IncidentExpire { get; set; }

        [JsonIgnore]
        public DateTime InvasionExpireTime { get; set; }

        [JsonPropertyName("grunt_type")]
        public InvasionCharacter GruntType { get; set; }

        [JsonPropertyName("last_modified")]
        public ulong LastModified { get; set; }

        [JsonPropertyName("updated")]
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

        public DiscordWebhookMessage GenerateEmbedMessage(AlarmMessageSettings settings)//, bool useLure, bool useInvasion)
        {
            var server = settings.Config.Instance.Servers[settings.GuildId];
            var embedType = HasInvasion ? EmbedMessageType.Invasions : HasLure ? EmbedMessageType.Lures : EmbedMessageType.Pokestops;
            var embed = settings.Alarm?.Embeds[embedType] ?? server.DmEmbeds?[embedType] ?? EmbedMessage.Defaults[embedType];
            var properties = GetProperties(settings);
            var eb = new DiscordEmbedBuilder
            {
                Title = DynamicReplacementEngine.ReplaceText(embed.Title, properties),
                Url = DynamicReplacementEngine.ReplaceText(embed.Url, properties),
                ImageUrl = DynamicReplacementEngine.ReplaceText(embed.ImageUrl, properties),
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = DynamicReplacementEngine.ReplaceText(embed.IconUrl, properties),
                },
                Description = DynamicReplacementEngine.ReplaceText(embed.Content, properties),
                /*
                TODO: Color = useInvasion
                    ? new DiscordColor(MasterFile.Instance.DiscordEmbedColors.Pokestops.Invasions)
                    : useLure
                        ? LureType.BuildLureColor(MasterFile.Instance.DiscordEmbedColors)
                        : DiscordColor.CornflowerBlue,
                */
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = DynamicReplacementEngine.ReplaceText(embed.Footer?.Text, properties),
                    IconUrl = DynamicReplacementEngine.ReplaceText(embed.Footer?.IconUrl, properties)
                }
            };
            var username = DynamicReplacementEngine.ReplaceText(embed.Username, properties);
            var iconUrl = DynamicReplacementEngine.ReplaceText(embed.AvatarUrl, properties);
            var description = DynamicReplacementEngine.ReplaceText(settings.Alarm?.Description, properties);
            return new DiscordWebhookMessage
            {
                Username = username,
                AvatarUrl = iconUrl,
                Content = description,
                Embeds = new List<DiscordEmbed> { eb.Build() },
            };
        }

        #endregion

        #region Private Methods

        private dynamic GetProperties(AlarmMessageSettings properties)
        {
            var lureImageUrl = IconFetcher.Instance.GetLureIcon(properties.Config.Instance.Servers[properties.GuildId].IconStyle, LureType);
            var invasionImageUrl = IconFetcher.Instance.GetInvasionIcon(properties.Config.Instance.Servers[properties.GuildId].IconStyle, GruntType);
            var imageUrl = HasInvasion ? invasionImageUrl : HasLure ? lureImageUrl : Url;
            var gmapsLink = string.Format(Strings.GoogleMaps, Latitude, Longitude);
            var appleMapsLink = string.Format(Strings.AppleMaps, Latitude, Longitude);
            var wazeMapsLink = string.Format(Strings.WazeMaps, Latitude, Longitude);
            var scannerMapsLink = string.Format(properties.Config.Instance.Urls.ScannerMap, Latitude, Longitude);
            var staticMapLink = StaticMap.GetUrl(properties.Config.Instance.Urls.StaticMap, HasInvasion
                ? properties.Config.Instance.StaticMaps["invasions"]
                : HasLure
                    ? properties.Config.Instance.StaticMaps["lures"] :
                    /* TODO: */"", Latitude, Longitude, imageUrl);
            var gmapsLocationLink = UrlShortener.CreateShortUrl(properties.Config.Instance.ShortUrlApiUrl, gmapsLink);
            var appleMapsLocationLink = UrlShortener.CreateShortUrl(properties.Config.Instance.ShortUrlApiUrl, appleMapsLink);
            var wazeMapsLocationLink = UrlShortener.CreateShortUrl(properties.Config.Instance.ShortUrlApiUrl, wazeMapsLink);
            var scannerMapsLocationLink = UrlShortener.CreateShortUrl(properties.Config.Instance.ShortUrlApiUrl, scannerMapsLink);
            // TODO: var address = new Coordinate(city, Latitude, Longitude).GetAddress(whConfig);
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
            var dict = new
            {
                //Main properties
                has_lure = Convert.ToString(HasLure),
                lure_type = LureType.ToString(),
                lure_expire_time = LureExpireTime.ToLongTimeString(),
                lure_expire_time_24h = LureExpireTime.ToString("HH:mm:ss"),
                lure_expire_time_left = lureExpireTimeLeft,
                has_invasion = HasInvasion,
                grunt_type = invasion?.Type,
                grunt_type_emoji = invasionTypeEmoji,
                grunt_gender = invasion?.Grunt,
                invasion_expire_time = InvasionExpireTime.ToLongTimeString(),
                invasion_expire_time_24h = InvasionExpireTime.ToString("HH:mm:ss"),
                invasion_expire_time_left = invasionExpireTimeLeft,
                invasion_encounters = $"**Encounter Reward Chance:**\r\n" + invasionEncounters,

                //Location properties
                geofence = properties.City ?? defaultMissingValue,
                lat = Latitude.ToString(),
                lng = Longitude.ToString(),
                lat_5 = Latitude.ToString("0.00000"),
                lng_5 = Longitude.ToString("0.00000"),

                //Location links
                tilemaps_url = staticMapLink,
                gmaps_url = gmapsLocationLink,
                applemaps_url = appleMapsLocationLink,
                wazemaps_url = wazeMapsLocationLink,
                scanmaps_url = scannerMapsLocationLink,

                //Pokestop properties
                pokestop_id = PokestopId ?? defaultMissingValue,
                pokestop_name = Name ?? defaultMissingValue,
                pokestop_url = Url ?? defaultMissingValue,
                lure_img_url = lureImageUrl,
                invasion_img_url = invasionImageUrl,

                //{ "address", address?.Address },

                // Discord Guild properties
                guild_name = "", // TODO: guild?.Name },
                guild_img_url = "", // TODO: guild?.IconUrl },

                //Misc properties
                date_time = DateTime.Now.ToString(),
                br = "\r\n",
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
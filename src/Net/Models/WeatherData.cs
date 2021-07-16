namespace WhMgr.Net.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;
    using ServiceStack.DataAnnotations;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Alarms.Models;
    using WhMgr.Configuration;
    using WhMgr.Data;
    using WhMgr.Extensions;
    using WhMgr.Geofence;
    using WhMgr.Localization;
    using WhMgr.Osm.Models;
    using WhMgr.Services;
    using WhMgr.Utilities;

    public enum WeatherSeverity
    {
        None = 0,
        Moderate,
        Extreme
    }

    /// <summary>
    /// RealDeviceMap Weather webhook model class.
    /// </summary>
    [Alias("weather")]
    public sealed class WeatherData
    {
        public const string WebhookHeader = "weather";

        #region Properties

        [JsonProperty("s2_cell_id")]
        public long Id { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("polygon")]
        public MultiPolygon Polygon { get; set; }

        [JsonProperty("gameplay_condition")]
        public WeatherCondition GameplayCondition { get; set; }

        [JsonProperty("wind_direction")]
        public int WindDirection { get; set; }

        [JsonProperty("cloud_level")]
        public ushort CloudLevel { get; set; }

        [JsonProperty("rain_level")]
        public ushort RainLevel { get; set; }

        [JsonProperty("wind_level")]
        public ushort WindLevel { get; set; }

        [JsonProperty("snow_level")]
        public ushort SnowLevel { get; set; }

        [JsonProperty("fog_level")]
        public ushort FogLevel { get; set; }

        [JsonProperty("special_effect_level")]
        public ushort SpecialEffectLevel { get; set; }

        [JsonProperty("severity")]
        public WeatherSeverity? Severity { get; set; }

        [JsonProperty("warn_weather")]
        public bool? WarnWeather { get; set; }

        [JsonProperty("updated")]
        public long Updated { get; set; }

        [JsonIgnore]
        public DateTime UpdatedTime { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiate a new <see cref="WeatherData"/> class.
        /// </summary>
        public WeatherData()
        {
            SetTimes();
        }

        #endregion

        #region Public Methods

        public void SetTimes()
        {
            UpdatedTime = Updated
                .FromUnix()
                .ConvertTimeFromCoordinates(Latitude, Longitude);
        }

        public DiscordEmbedNotification GenerateWeatherMessage(ulong guildId, DiscordClient client, WhConfig whConfig, AlarmObject alarm, string city)
        {
            var server = whConfig.Servers[guildId];
            var alertType = AlertMessageType.Weather;
            var alert = alarm?.Alerts[alertType] ?? server.DmAlerts?[alertType] ?? AlertMessage.Defaults[alertType];
            var weatherImageUrl = IconFetcher.Instance.GetWeatherIcon(server.IconStyle, GameplayCondition);
            var properties = GetProperties(client.Guilds[guildId], whConfig, city, weatherImageUrl);
            var eb = new DiscordEmbedBuilder
            {
                Title = DynamicReplacementEngine.ReplaceText(alert.Title, properties),
                Url = DynamicReplacementEngine.ReplaceText(alert.Url, properties),
                ImageUrl = DynamicReplacementEngine.ReplaceText(alert.ImageUrl, properties),
                ThumbnailUrl = DynamicReplacementEngine.ReplaceText(alert.IconUrl, properties),
                Description = DynamicReplacementEngine.ReplaceText(alert.Content, properties),
                Color = GameplayCondition.BuildWeatherColor(MasterFile.Instance.DiscordEmbedColors),
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

        private IReadOnlyDictionary<string, string> GetProperties(DiscordGuild guild, WhConfig whConfig, string city, string weatherImageUrl)
        {
            var weather = Translator.Instance.GetWeather(GameplayCondition);
            var weatherEmoji = GameplayCondition != WeatherCondition.None ? GameplayCondition.GetEmojiIcon("weather", false) : string.Empty;
            var hasWeather = GameplayCondition != WeatherCondition.None;
            var gmapsLink = string.Format(Strings.GoogleMaps, Latitude, Longitude);
            var appleMapsLink = string.Format(Strings.AppleMaps, Latitude, Longitude);
            var wazeMapsLink = string.Format(Strings.WazeMaps, Latitude, Longitude);
            var scannerMapsLink = string.Format(whConfig.Urls.ScannerMap, Latitude, Longitude);
            var staticMapLink = StaticMap.GetUrl(whConfig.Urls.StaticMap, whConfig.StaticMaps["weather"], Latitude, Longitude, weatherImageUrl, PokemonTeam.All, null, Polygon);
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
                { "id", Id.ToString() },
                { "weather_condition", weather },
                { "has_weather", Convert.ToString(hasWeather) },
                { "weather", weather ?? defaultMissingValue },
                { "weather_emoji", weatherEmoji ?? defaultMissingValue },
                { "weather_img_url", weatherImageUrl },

                { "wind_direction", WindDirection.ToString() },
                { "wind_level", WindLevel.ToString() },
                { "raid_level", RainLevel.ToString() },
                { "cloud_level", CloudLevel.ToString() },
                { "fog_level", FogLevel.ToString() },
                { "snow_level", SnowLevel.ToString() },
                { "warn_weather", Convert.ToString(WarnWeather ?? false) },
                { "special_effect_level", SpecialEffectLevel.ToString() },
                { "severity", Severity.ToString() },

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

        public static MultiPolygon FixWeatherPolygon(MultiPolygon multiPolygon)
        {
            var newMultiPolygon = new MultiPolygon();
            if (multiPolygon.Count == 0 || multiPolygon == null)
                return newMultiPolygon;

            multiPolygon.ForEach(x => newMultiPolygon.Add(new Polygon { x[1], x[0] }));
            newMultiPolygon.Add(newMultiPolygon[newMultiPolygon.Count - 1]);
            return newMultiPolygon;
        }

        #endregion
    }
}
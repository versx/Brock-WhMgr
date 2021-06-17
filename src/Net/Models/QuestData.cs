namespace WhMgr.Net.Models
{
    using System;
    using System.Collections.Generic;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;
    using POGOProtos.Rpc;
    using ItemId = POGOProtos.Rpc.Item;
    using ActivityType = POGOProtos.Rpc.HoloActivityType;
    using QuestConditionType = POGOProtos.Rpc.QuestConditionProto.Types.ConditionType;
    using QuestRewardType = POGOProtos.Rpc.QuestRewardProto.Types.Type;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Alarms.Models;
    using WhMgr.Configuration;
    using WhMgr.Extensions;
    using WhMgr.Geofence;
    using WhMgr.Services;
    using WhMgr.Utilities;

    /// <summary>
    /// RealDeviceMap Quest webhook model class.
    /// </summary>
    public sealed class QuestData
    {
        public const string WebHookHeader = "quest";

        //private static readonly IEventLogger _logger = EventLogger.GetLogger("QUESTDATA", Program.LogLevel);

        #region Properties

        [JsonProperty("pokestop_id")]
        public string PokestopId { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("pokestop_name")]
        public string PokestopName { get; set; }

        [JsonProperty("pokestop_url")]
        public string PokestopUrl { get; set; }

        [JsonProperty("type")]
        public QuestType Type { get; set; }

        [JsonProperty("target")]
        public int Target { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }

        [JsonProperty("updated")]
        public long Updated { get; set; }

        [JsonProperty("rewards")]
        public List<QuestRewardMessage> Rewards { get; set; }

        [JsonProperty("conditions")]
        public List<QuestConditionMessage> Conditions { get; set; }

        [JsonIgnore]
        public bool IsDitto => Rewards?[0]?.Info?.Ditto ?? false;

        [JsonIgnore]
        public bool IsShiny => Rewards?[0]?.Info?.Shiny ?? false;

        #endregion

        /// <summary>
        /// Instantiate a new <see cref="QuestData"/> class.
        /// </summary>
        public QuestData()
        {
            Rewards = new List<QuestRewardMessage>();
            Conditions = new List<QuestConditionMessage>();
        }

        /// <summary>
        /// Generates a Discord embed message for a Pokestop Quest
        /// </summary>
        /// <param name="guildId">Discord Guild ID related to the data</param>
        /// <param name="client">Discord client to use</param>
        /// <param name="whConfig">Config to use</param>
        /// <param name="alarm">Alarm to use</param>
        /// <param name="city">City to specify</param>
        /// <returns></returns>
        public DiscordEmbedNotification GenerateQuestMessage(ulong guildId, DiscordClient client, WhConfig whConfig, AlarmObject alarm, string city)
        {
            var server = whConfig.Servers[guildId];
            var alertType = AlertMessageType.Quests;
            var alert = alarm?.Alerts[alertType] ?? server.DmAlerts?[alertType] ?? AlertMessage.Defaults[alertType];
            var properties = GetProperties(client.Guilds[guildId], whConfig, city, IconFetcher.Instance.GetQuestIcon(whConfig.Servers[guildId].IconStyle, this));
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
                Color = new DiscordColor(server.DiscordEmbedColors.Pokestops.Quests),
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

        private IReadOnlyDictionary<string, string> GetProperties(DiscordGuild guild, WhConfig whConfig, string city, string questRewardImageUrl)
        {
            var questMessage = this.GetQuestMessage();
            var questConditions = this.GetConditions();
            var questReward = this.GetReward();
            var gmapsLink = string.Format(Strings.GoogleMaps, Latitude, Longitude);
            var appleMapsLink = string.Format(Strings.AppleMaps, Latitude, Longitude);
            var wazeMapsLink = string.Format(Strings.WazeMaps, Latitude, Longitude);
            var scannerMapsLink = string.Format(whConfig.Urls.ScannerMap, Latitude, Longitude);
            var staticMapLink = StaticMap.GetUrl(whConfig.Urls.StaticMap, whConfig.StaticMaps["quests"], Latitude, Longitude, questRewardImageUrl);
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
                { "quest_task", questMessage },
                { "quest_conditions", questConditions },
                { "quest_reward", questReward },
                { "quest_reward_img_url", questRewardImageUrl },
                { "has_quest_conditions", Convert.ToString(!string.IsNullOrEmpty(questConditions)) },
                { "is_ditto", Convert.ToString(IsDitto) },
                { "is_shiny", Convert.ToString(IsShiny) },

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

                //Pokestop properties
                { "pokestop_id", PokestopId ?? defaultMissingValue },
                { "pokestop_name", PokestopName ?? defaultMissingValue },
                { "pokestop_url", PokestopUrl ?? defaultMissingValue },

                // Discord Guild properties
                { "guild_name", guild?.Name },
                { "guild_img_url", guild?.IconUrl },

                { "date_time", DateTime.Now.ToString() },

                //Misc properties
                { "br", "\r\n" }
            };
            return dict;
        }
    }

    public sealed class QuestConditionMessage
    {
        [JsonProperty("type")]
        public QuestConditionType Type { get; set; }

        [JsonProperty("info")]
        public QuestCondition Info { get; set; }

        public QuestConditionMessage()
        {
            Type = QuestConditionType.Unset;
        }
    }

    public sealed class QuestCondition
    {
        [JsonProperty("pokemon_ids")]
        public List<uint> PokemonIds { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("pokemon_type_ids")]
        public List<int> PokemonTypeIds { get; set; }

        [JsonProperty("throw_type_id")]
        public ActivityType ThrowTypeId { get; set; }

        [JsonProperty("hit")]
        public bool Hit { get; set; }

        [JsonProperty("raid_levels")]
        public List<int> RaidLevels { get; set; }

        [JsonProperty("alignment_ids")]
        public List<int> AlignmentIds { get; set; }

        [JsonProperty("character_category_ids")]
        public List<int> CharacterCategoryIds { get; set; }

        [JsonProperty("raid_pokemon_evolutions")]
        public List<int> RaidPokemonEvolutions { get; set; }

        public QuestCondition()
        {
            ThrowTypeId = ActivityType.ActivityUnknown;
        }
    }

    public sealed class QuestRewardMessage
    {
        [JsonProperty("type")]
        public QuestRewardType Type { get; set; }

        [JsonProperty("info")]
        public QuestReward Info { get; set; }

        public QuestRewardMessage()
        {
            Type = QuestRewardType.Unset;
        }
    }

    public sealed class QuestReward
    {
        [JsonProperty("pokemon_id")]
        public uint PokemonId { get; set; }

        [JsonProperty("costume_id")]
        public int CostumeId { get; set; }

        [JsonProperty("form_id")]
        public int FormId { get; set; }

        [JsonProperty("gender_id")]
        public int GenderId { get; set; }

        [JsonProperty("ditto")]
        public bool Ditto { get; set; }

        [JsonProperty("shiny")]
        public bool Shiny { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("item_id")]
        public ItemId Item { get; set; }

        [JsonProperty("raid_levels")]
        public List<int> RaidLevels { get; set; }

        [JsonProperty("mega_resource")]
        public QuestMegaResource MegaResource { get; set; }

        [JsonProperty("sticker_id")]
        public string StickerId { get; set; }

        // TODO: Pokemon alignment
    }

    public sealed class QuestMegaResource
    {
        public ushort PokemonId { get; set; }

        public int Amount { get; set; }
    }
}
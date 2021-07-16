namespace WhMgr.Net.Models
{
    using System;
    using System.Collections.Generic;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;
    using Gender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Alarms.Models;
    using WhMgr.Configuration;
    using WhMgr.Data;
    using WhMgr.Data.Models;
    using WhMgr.Extensions;
    using WhMgr.Geofence;
    using WhMgr.Localization;
    using WhMgr.Services;
    using WhMgr.Utilities;

    /// <summary>
    /// RealDeviceMap Raid/Egg webhook model class.
    /// </summary>
    public sealed class RaidData
    {
        public const string WebHookHeader = "raid";

        #region Properties

        [JsonProperty("gym_id")]
        public string GymId { get; set; }

        [JsonProperty("gym_name")]
        public string GymName { get; set; }

        [JsonProperty("gym_url")]
        public string GymUrl { get; set; }

        [JsonProperty("pokemon_id")]
        public uint PokemonId { get; set; }

        [JsonProperty("team_id")]
        public PokemonTeam Team { get; set; } = PokemonTeam.Neutral;

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("cp")]
        public string CP { get; set; }

        [JsonProperty("move_1")]
        public int FastMove { get; set; }

        [JsonProperty("move_2")]
        public int ChargeMove { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("start")]
        public long Start { get; set; }

        [JsonProperty("end")]
        public long End { get; set; }

        [JsonProperty("ex_raid_eligible")]
        public bool IsExEligible { get; set; }

        [JsonProperty("sponsor_id")]
        public bool SponsorId { get; set; }

        [JsonProperty("form")]
        public int Form { get; set; }

        [JsonProperty("costume")]
        public int Costume { get; set; }

        [JsonProperty("evolution")]
        public int Evolution { get; set; }

        [JsonProperty("gender")]
        public Gender Gender { get; set; }

        [JsonIgnore]
        public DateTime StartTime { get; private set; }

        [JsonIgnore]
        public DateTime EndTime { get; private set; }

        [JsonIgnore]
        public bool IsEgg => PokemonId == 0;

        [JsonIgnore]
        public List<PokemonType> Weaknesses
        {
            get
            {
                if (MasterFile.Instance.Pokedex.ContainsKey(PokemonId) && !IsEgg)
                {
                    var list = new List<PokemonType>();
                    var types = MasterFile.GetPokemon(PokemonId, Form)?.Types;
                    if (types != null)
                    {
                        MasterFile.GetPokemon(PokemonId, Form)?.Types?.ForEach(x => list.AddRange(x.GetWeaknesses()));
                    }
                    return list;
                }

                return null;
            }
        }

        [JsonIgnore]
        public bool IsMissingStats => FastMove == 0 || ChargeMove == 0;

        #endregion

        /// <summary>
        /// Instantiate a new <see cref="RaidData"/> class.
        /// </summary>
        public RaidData()
        {
            SetTimes();
        }

        /// <summary>
        /// Set expire times because .NET doesn't support Unix timestamp deserialization to <seealso cref="DateTime"/> class by default.
        /// </summary>
        public void SetTimes()
        {
            StartTime = Start
                .FromUnix()
                .ConvertTimeFromCoordinates(Latitude, Longitude);

            EndTime = End
                .FromUnix()
                .ConvertTimeFromCoordinates(Latitude, Longitude);
        }

        /// <summary>
        /// Generate a Discord embed Raid message
        /// </summary>
        /// <param name="guildId">Guild the notification is for</param>
        /// <param name="client">Discord client</param>
        /// <param name="whConfig">Webhook config</param>
        /// <param name="alarm">Webhook alarm</param>
        /// <param name="city">City the Raid was found in</param>
        /// <returns>DiscordEmbedNotification object to send</returns>
        public DiscordEmbedNotification GenerateRaidMessage(ulong guildId, DiscordClient client, WhConfig whConfig, AlarmObject alarm, string city)
        {
            var server = whConfig.Servers[guildId];
            var alertType = PokemonId > 0 ? AlertMessageType.Raids : AlertMessageType.Eggs;
            var alert = alarm?.Alerts[alertType] ?? server.DmAlerts?[alertType] ?? AlertMessage.Defaults[alertType];
            var raidImageUrl = IsEgg ?
                IconFetcher.Instance.GetRaidEggIcon(server.IconStyle, Convert.ToInt32(Level), false, IsExEligible) :
                IconFetcher.Instance.GetPokemonIcon(server.IconStyle, PokemonId, Form, Evolution, Gender, Costume, false);
            var properties = GetProperties(client.Guilds[guildId], whConfig, city, raidImageUrl);
            var eb = new DiscordEmbedBuilder
            {
                Title = DynamicReplacementEngine.ReplaceText(alert.Title, properties),
                Url = DynamicReplacementEngine.ReplaceText(alert.Url, properties),
                ImageUrl = DynamicReplacementEngine.ReplaceText(alert.ImageUrl, properties),
                ThumbnailUrl = DynamicReplacementEngine.ReplaceText(alert.IconUrl, properties),
                Description = DynamicReplacementEngine.ReplaceText(alert.Content, properties),
                Color = (IsExEligible ? 0 /*ex*/ : int.Parse(Level)).BuildRaidColor(MasterFile.Instance.DiscordEmbedColors),
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

        private IReadOnlyDictionary<string, string> GetProperties(DiscordGuild guild, WhConfig whConfig, string city, string raidImageUrl)
        {
            var pkmnInfo = MasterFile.GetPokemon(PokemonId, Form);
            var name = IsEgg ? "Egg" /*TODO: Localize*/ : Translator.Instance.GetPokemonName(PokemonId);
            var form = Translator.Instance.GetFormName(Form);
            var costume = Translator.Instance.GetCostumeName(Costume);
            var evo = Translator.Instance.GetEvolutionName(Evolution);
            var gender = Gender.GetPokemonGenderIcon();
            var level = Level;
            var move1 = Translator.Instance.GetMoveName(FastMove);
            var move2 = Translator.Instance.GetMoveName(ChargeMove);
            var types = pkmnInfo?.Types;
            var type1 = types?[0];
            var type2 = types?.Count > 1 ? types?[1] : PokemonType.None;
            var type1Emoji = types?[0].GetTypeEmojiIcons();
            var type2Emoji = pkmnInfo?.Types?.Count > 1 ? types?[1].GetTypeEmojiIcons() : string.Empty;
            var typeEmojis = $"{type1Emoji} {type2Emoji}";
            var weaknesses = Weaknesses == null ? string.Empty : string.Join(", ", Weaknesses);
            var weaknessesEmoji = types?.GetWeaknessEmojiIcons();
            var perfectRange = PokemonId.MaxCpAtLevel(20);
            var boostedRange = PokemonId.MaxCpAtLevel(25);
            var worstRange = PokemonId.MinCpAtLevel(20);
            var worstBoosted = PokemonId.MinCpAtLevel(25);
            var exEmojiId = MasterFile.Instance.Emojis["ex"];
            var exEmoji = exEmojiId > 0 ? $"<:ex:{exEmojiId}>" : "EX";
            var teamEmojiId = MasterFile.Instance.Emojis[Team.ToString().ToLower()];
            var teamEmoji = teamEmojiId > 0 ? $"<:{Team.ToString().ToLower()}:{teamEmojiId}>" : Team.ToString();

            var gmapsLink = string.Format(Strings.GoogleMaps, Latitude, Longitude);
            var appleMapsLink = string.Format(Strings.AppleMaps, Latitude, Longitude);
            var wazeMapsLink = string.Format(Strings.WazeMaps, Latitude, Longitude);
            var scannerMapsLink = string.Format(whConfig.Urls.ScannerMap, Latitude, Longitude);
            var staticMapLink = StaticMap.GetUrl(whConfig.Urls.StaticMap, whConfig.StaticMaps["raids"], Latitude, Longitude, raidImageUrl, Team);
            var gmapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, gmapsLink);
            var appleMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, appleMapsLink);
            var wazeMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, wazeMapsLink);
            var scannerMapsLocationLink = UrlShortener.CreateShortUrl(whConfig.ShortUrlApiUrl, scannerMapsLink);
            var address = new Location(null, city, Latitude, Longitude).GetAddress(whConfig);
            //var staticMapLocationLink = string.IsNullOrEmpty(whConfig.ShortUrlApiUrl) ? staticMapLink : NetUtil.CreateShortUrl(whConfig.ShortUrlApiUrl, staticMapLink);

            var now = DateTime.UtcNow.ConvertTimeFromCoordinates(Latitude, Longitude);
            var startTimeLeft = now.GetTimeRemaining(StartTime).ToReadableStringNoSeconds();
            var endTimeLeft = now.GetTimeRemaining(EndTime).ToReadableStringNoSeconds();

            const string defaultMissingValue = "?";
            var dict = new Dictionary<string, string>
            {
                //Raid boss properties
                { "pkmn_id", PokemonId.ToString() },
                { "pkmn_id_3", PokemonId.ToString("D3") },
                { "pkmn_name", name },
                { "pkmn_img_url", raidImageUrl },
                { "evolution", evo },
                { "evolution_id", Convert.ToInt32(Evolution).ToString() },
                { "evolution_id_3", Evolution.ToString("D3") },
                { "form", form },
                { "form_id", Form.ToString() },
                { "form_id_3", Form.ToString("D3") },
                { "costume", costume },
                { "costume_id", Costume.ToString() },
                { "costume_id_3", Costume.ToString("D3") },
                { "is_egg", Convert.ToString(IsEgg) },
                { "is_ex", Convert.ToString(IsExEligible) },
                { "ex_emoji", exEmoji },
                { "team", Team.ToString() },
                { "team_id", Convert.ToInt32(Team).ToString() },
                { "team_emoji", teamEmoji },
                { "cp", CP ?? defaultMissingValue },
                { "lvl", level ?? defaultMissingValue },
                { "gender", gender ?? defaultMissingValue },
                { "move_1", move1 ?? defaultMissingValue },
                { "move_2", move2 ?? defaultMissingValue },
                { "moveset", $"{move1}/{move2}" },
                { "type_1", type1?.ToString() ?? defaultMissingValue },
                { "type_2", type2?.ToString() ?? defaultMissingValue },
                { "type_1_emoji", type1Emoji },
                { "type_2_emoji", type2Emoji },
                { "types", $"{type1}/{type2}" },
                { "types_emoji", typeEmojis },
                { "weaknesses", weaknesses },
                { "weaknesses_emoji", weaknessesEmoji },
                { "perfect_cp", perfectRange.ToString() },
                { "perfect_cp_boosted", boostedRange.ToString() },
                { "worst_cp", worstRange.ToString() },
                { "worst_cp_boosted", worstBoosted.ToString() },

                //Time properties
                { "start_time", StartTime.ToLongTimeString() },
                { "start_time_24h", StartTime.ToString("HH:mm:ss") },
                { "start_time_left", startTimeLeft },
                { "end_time", EndTime.ToLongTimeString() },
                { "end_time_24h", EndTime.ToString("HH:mm:ss") },
                { "end_time_left", endTimeLeft },

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

                //Gym properties
                { "gym_id", GymId },
                { "gym_name", GymName },
                { "gym_url", GymUrl },

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
}

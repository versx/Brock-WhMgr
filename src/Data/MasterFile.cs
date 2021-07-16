namespace WhMgr.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using POGOProtos.Rpc;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;

    using WhMgr.Configuration;
    using WhMgr.Data.Models;
    using WhMgr.Diagnostics;
    using WhMgr.Net.Models;

    public class MasterFile
    {
        const string MasterFileName = "masterfile.json";
        const string CpMultipliersFileName = "cpMultipliers.json";
        const string EmojisFileName = "emojis.json";
        const string RarityFileName = "rarity.json";
        const string EmbedColorsFileName = "embedColors.json";

        private static readonly IEventLogger _logger = EventLogger.GetLogger("MASTER", Program.LogLevel);

        #region Properties

        [JsonProperty("pokemon")]
        public IReadOnlyDictionary<uint, PokedexPokemon> Pokedex { get; set; }

        //[JsonProperty("moves")]
        //public IReadOnlyDictionary<int, Moveset> Movesets { get; set; }

        [JsonProperty("quest_conditions")]
        public IReadOnlyDictionary<string, QuestConditionModel> QuestConditions { get; set; }

        [JsonProperty("quest_types")]
        public IReadOnlyDictionary<int, QuestTypeModel> QuestTypes { get; set; }

        [JsonProperty("quest_reward_types")]
        public IReadOnlyDictionary<int, QuestRewardTypeModel> QuestRewardTypes { get; set; }

        [JsonProperty("throw_types")]
        public IReadOnlyDictionary<int, string> ThrowTypes { get; set; }

        [JsonProperty("items")]
        public IReadOnlyDictionary<int, ItemModel> Items { get; set; }

        [JsonProperty("grunt_types")]
        public IReadOnlyDictionary<InvasionCharacter, TeamRocketInvasion> GruntTypes { get; set; }

        [JsonProperty("pokemon_types")]
        public IReadOnlyDictionary<PokemonType, PokemonTypes> PokemonTypes { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<double, double> CpMultipliers { get; }

        [JsonIgnore]
        public Dictionary<string, ulong> Emojis { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> CustomEmojis { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<PokemonRarity, List<uint>> PokemonRarity { get; set; }

        /// <summary>
        /// Gets or sets the Discord embed colors to use for each message type
        /// </summary>
        [JsonIgnore]
        public DiscordEmbedColorConfig DiscordEmbedColors { get; set; }

        #region Singletons

        private static MasterFile _instance;
        public static MasterFile Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadInit<MasterFile>(Path.Combine(Strings.DataFolder, MasterFileName));
                }

                return _instance;
            }
        }

        #endregion

        #endregion

        public MasterFile()
        {
            CpMultipliers = LoadInit<Dictionary<double, double>>(Path.Combine(Strings.DataFolder, CpMultipliersFileName));
            PokemonRarity = LoadInit<Dictionary<PokemonRarity, List<uint>>>(Path.Combine(Strings.DataFolder, RarityFileName));
            Emojis = new Dictionary<string, ulong>();
            CustomEmojis = LoadInit<Dictionary<string, string>>(Path.Combine(Strings.DataFolder, EmojisFileName));
            DiscordEmbedColors = LoadInit<DiscordEmbedColorConfig>(Path.Combine(Strings.DataFolder, EmbedColorsFileName));
        }

        public static PokedexPokemon GetPokemon(uint pokemonId, int formId)
        {
            if (!Instance.Pokedex.ContainsKey(pokemonId))
                return null;

            var pkmn = Instance.Pokedex[pokemonId];
            var useForm = !pkmn.Attack.HasValue && formId > 0 && (pkmn.Forms?.ContainsKey(formId) ?? true);
            var pkmnForm = useForm ? pkmn.Forms[formId] : pkmn;
            pkmnForm.Name = pkmn.Name;
            return pkmnForm;
        }

        public static T LoadInit<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                _logger.Error($"{filePath} database is empty.");
                return default;
            }

            return (T)JsonConvert.DeserializeObject(data, typeof(T));
        }
    }

    public class PokemonTypes
    {
        [JsonProperty("immunes")]
        public List<PokemonType> Immune { get; set; }

        [JsonProperty("weaknesses")]
        public List<PokemonType> Weaknesses { get; set; }

        [JsonProperty("resistances")]
        public List<PokemonType> Resistances { get; set; }

        [JsonProperty("strengths")]
        public List<PokemonType> Strengths { get; set; }

        public PokemonTypes()
        {
            Immune = new List<PokemonType>();
            Weaknesses = new List<PokemonType>();
            Resistances = new List<PokemonType>();
            Strengths = new List<PokemonType>();
        }
    }

    public class Emoji
    {
        public ulong GuildId { get; set; }

        public string Name { get; set; }

        public ulong Id { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PokemonRarity
    {
        Common,
        Rare
    }

    public class ItemModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("proto")]
        public string ProtoName { get; set; }

        [JsonProperty("min_trainer_level")]
        public int MinimumTrainerLevel { get; set; }
    }

    public class QuestTypeModel
    {
        [JsonProperty("prototext")]
        public string ProtoText { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class QuestConditionModel
    {
        [JsonProperty("prototext")]
        public string ProtoText { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class QuestRewardTypeModel
    {
        [JsonProperty("prototext")]
        public string ProtoText { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
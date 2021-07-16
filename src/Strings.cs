namespace WhMgr
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using POGOProtos.Rpc;
    using WhMgr.Data.Models;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    /// <summary>
    /// Static strings class
    /// </summary>
    internal static class Strings
    {
        public const string BotName = "Brock";
        public const string Creator = "versx";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public const string BannerAsciiText = @"
 __      __      ___.   .__                   __     
/  \    /  \ ____\_ |__ |  |__   ____   ____ |  | __ 
\   \/\/   // __ \| __ \|  |  \ /  _ \ /  _ \|  |/ / 
 \        /\  ___/| \_\ \   Y  (  <_> |  <_> )    <  
  \__/\  /  \___  >___  /___|  /\____/ \____/|__|_ \ 
       \/       \/    \/     \/                   \/ 
   _____                                             
  /     \ _____    ____ _____     ____   ___________ 
 /  \ /  \\__  \  /    \\__  \   / ___\_/ __ \_  __ \
/    Y    \/ __ \|   |  \/ __ \_/ /_/  >  ___/|  | \/
\____|__  (____  /___|  (____  /\___  / \___  >__|   
        \/     \/     \/     \//_____/      \/       
        ";

        public const string GoogleMaps = "https://maps.google.com/maps?q={0},{1}";
        public const string AppleMaps = "https://maps.apple.com/maps?daddr={0},{1}";
        public const string WazeMaps = "https://waze.com/ul?ll={0},{1}&navigate=yes";

        public const string GeofenceFolder = "geofences";
        public const string AlarmsFolder = "alarms";
        public const string AlertsFolder = "alerts";
        public const string DiscordsFolder = "discords";
        public const string FiltersFolder = "filters";
        public const string LibrariesFolder = "libs";
        public const string StaticFolder = "static";
        public const string TemplatesFolder = "templates";
        public const string MigrationsFolder = "migrations";
        public static readonly string AppFolder = StaticFolder + Path.DirectorySeparatorChar + "app";
        public static readonly string DataFolder = StaticFolder + Path.DirectorySeparatorChar + "data";
        public static readonly string LocaleFolder = StaticFolder + Path.DirectorySeparatorChar + "locales";
        public static readonly string EmojisFolder = StaticFolder + Path.DirectorySeparatorChar + "emojis";
        public static readonly string OsmNestFilePath = StaticFolder + Path.DirectorySeparatorChar + OsmNestFileName;
        public const string StatsFolder = "stats";
        public const string LogsFolder = "logs";

        public const string DefaultResponseMessage = "WH Test Running!";
        public static readonly string[] LocalEndPoint = { "localhost", "127.0.0.1" };

        public const string ConfigFileName = "config.json";
        public const string OsmNestFileName = "nest.json";
        public const string DebugLogFileName = "debug.log";
        public const string ErrorLogFileName = "error.log";
        public const string StatsFileName = "notifications_{0}.csv";

        public static readonly List<string> ValidGenders = new List<string> { "*", "m", "f" };

        // Default filter settings for alarms and subscriptions
        public const int MinimumIV = 0;
        public const int MaximumIV = 100;
        public const int MinimumLevel = 0;
        public const int MaximumLevel = 35;
        public const int MinimumCP = 0;
        public const int MaximumCP = 99999;
        public const int MinimumRank = 0;
        public const int MaximumRank = 100;
        public const int MinimumPercent = 0;
        public const int MaximumPercent = 100;
        public const int MinimumGreatLeagueCP = 1400;
        public const int MaximumGreatLeagueCP = 1500;
        public const int MinimumUltraLeagueCP = 2400;
        public const int MaximumUltraLeagueCP = 2500;

        public const string All = "All";

        public const int MaxPokemonDisplayed = 70;
        public const int MaxPokemonSubscriptions = 25;
        public const int MaxPvPSubscriptions = 15;
        public const int MaxRaidSubscriptions = 5;
        public const int MaxGymSubscriptions = 3;
        public const int MaxQuestSubscriptions = 2;
        public const int MaxInvasionSubscriptions = 1;
        public const int CommonTypeMinimumIV = 90;

        public const int MaxQueueCountWarning = 30;

        public const string EmojiSchema = "<:{0}:{1}>";
        public const string TypeEmojiSchema = "<:types_{0}:{1}>";

        public const string SQL_SELECT_CONVERTED_POKESTOPS = "SELECT pokestop.id, pokestop.lat, pokestop.lon, pokestop.name, pokestop.url FROM pokestop INNER JOIN gym ON pokestop.id = gym.id WHERE pokestop.id = gym.id;";
        public const string SQL_UPDATE_CONVERTED_POKESTOPS = "UPDATE gym INNER JOIN pokestop ON pokestop.id = gym.id SET gym.name = pokestop.name, gym.url = pokestop.url;";
        public const string SQL_DELETE_CONVERTED_POKESTOPS = "DELETE FROM pokestop WHERE id IN (SELECT id FROM gym)";
        public const string SQL_DELETE_STALE_POKESTOPS = "DELETE FROM pokestop WHERE updated < UNIX_TIMESTAMP() - 90000;";

        public static readonly Dictionary<int, PokemonGenerationRange> PokemonGenerationRanges = new Dictionary<int, PokemonGenerationRange>
        {
            { 1, new PokemonGenerationRange { Generation = 1, Start = 1, End = 151 } },
            { 2, new PokemonGenerationRange { Generation = 2, Start = 152, End = 251 } },
            { 3, new PokemonGenerationRange { Generation = 3, Start = 252, End = 385 } },
            { 4, new PokemonGenerationRange { Generation = 4, Start = 386, End = 493 } },
            { 5, new PokemonGenerationRange { Generation = 5, Start = 495, End = 649 } },
            { 6, new PokemonGenerationRange { Generation = 6, Start = 650, End = 721 } },
            { 7, new PokemonGenerationRange { Generation = 7, Start = 722, End = 809 } },
            { 8, new PokemonGenerationRange { Generation = 8, Start = 810, End = 890 } }
        };

        // Required emoji list
        public static readonly string[] EmojiList =
        {
            // Team emojis
            "neutral",
            "valor",
            "mystic",
            "instinct",

            // Capture rate emojis
            "capture_1",
            "capture_2",
            "capture_3",

            // Weather emojis
            "weather_1", // Clear
            "weather_2", // Rain
            "weather_3", // PartlyCloudy/Overcast
            "weather_4", // Cloudy
            "weather_5", // Windy
            "weather_6", // Snow
            "weather_7", // Fog

            // Ex gym emoji
            "ex",

            // Type emojis
            "types_fire",
            "types_grass",
            "types_ground",
            "types_rock",
            "types_water",
            "types_ghost",
            "types_ice",
            "types_dragon",
            "types_fairy",
            "types_fighting",
            "types_bug",
            "types_psychic",
            "types_electric",
            "types_steel",
            "types_dark",
            "types_normal",
            "types_flying",
            "types_poison",

            // PVP league emojis
            "league_great",
            "league_ultra",

            // Gender emojis
            "gender_male",
            "gender_female",
            "gender_less"
        };

        public static IReadOnlyDictionary<WeatherCondition, List<PokemonType>> WeatherBoosts => new Dictionary<WeatherCondition, List<PokemonType>>
        {
            { WeatherCondition.None,         new List<PokemonType> { } },
            { WeatherCondition.Clear,        new List<PokemonType> { PokemonType.Fire,   PokemonType.Grass,    PokemonType.Ground } },
            { WeatherCondition.Rainy,        new List<PokemonType> { PokemonType.Water,  PokemonType.Electric, PokemonType.Bug } },
            { WeatherCondition.PartlyCloudy, new List<PokemonType> { PokemonType.Normal, PokemonType.Rock } },
            { WeatherCondition.Overcast,     new List<PokemonType> { PokemonType.Fairy,  PokemonType.Fighting, PokemonType.Poison } },
            { WeatherCondition.Windy,        new List<PokemonType> { PokemonType.Dragon, PokemonType.Flying,   PokemonType.Psychic } },
            { WeatherCondition.Snow,         new List<PokemonType> { PokemonType.Ice,    PokemonType.Steel } },
            { WeatherCondition.Fog,          new List<PokemonType> { PokemonType.Dark,   PokemonType.Ghost } }
        };
    }

    /// <summary>
    /// Pokemon generation range class
    /// </summary>
    public class PokemonGenerationRange
    {
        /// <summary>
        /// Gets or sets the Pokemon generation number
        /// </summary>
        public int Generation { get; set; }

        /// <summary>
        /// Gets or sets the pokedex ID the generation starts at
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the pokedex ID the generation ends at
        /// </summary>
        public int End { get; set; }
    }
}

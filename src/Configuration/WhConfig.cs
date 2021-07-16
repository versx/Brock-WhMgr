namespace WhMgr.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json;

    using WhMgr.Data;
    using WhMgr.Diagnostics;

    /// <summary>
    /// Configuration file class
    /// </summary>
    public class WhConfig
    {
        /// <summary>
        /// Gets or sets the HTTP listening interface/host address
        /// </summary>
        [JsonProperty("host")]
        public string ListeningHost { get; set; }

        /// <summary>
        /// Gets or sets the HTTP listening port
        /// </summary>
        [JsonProperty("port")]
        public ushort WebhookPort { get; set; }

        /// <summary>
        /// Gets or sets the locale translation file to use
        /// </summary>
        [JsonProperty("locale")]
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the short url API url (yourls.org)
        /// </summary>
        [JsonProperty("shortUrlApiUrl")]
        public string ShortUrlApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the Stripe API key
        /// </summary>
        [JsonProperty("stripeApiKey")]
        public string StripeApiKey { get; set; }

        /// <summary>
        /// Gets or sets the Discord servers configuration
        /// </summary>
        [JsonIgnore]
        public Dictionary<ulong, DiscordServerConfig> Servers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("servers")]
        public Dictionary<string, string> ServerConfigFiles { get; set; }

        /// <summary>
        /// Gets or sets the Database configuration
        /// </summary>
        [JsonProperty("database")]
        public ConnectionStringsConfig Database { get; set; }

        /// <summary>
        /// Gets or sets the Urls configuration
        /// </summary>
        [JsonProperty("urls")]
        public UrlConfig Urls { get; set; }

        /// <summary>
        /// Gets or sets the event Pokemon IDs list
        /// </summary>
        [JsonProperty("eventPokemonIds")]
        public List<uint> EventPokemonIds { get; set; }

        /// <summary>
        /// Gets or sets the minimum IV value for an event Pokemon to be to process
        /// for channel alarms or direct message subscriptions
        /// </summary>
        [JsonProperty("eventMinimumIV")]
        public int EventMinimumIV { get; set; }

        /// <summary>
        /// Gets or sets the icon styles
        /// </summary>
        [JsonProperty("iconStyles")]
        public Dictionary<string, string> IconStyles { get; set; }

        /// <summary>
        /// Gets or sets the static map template files to use per type
        /// </summary>
        [JsonProperty("staticMaps")]
        public Dictionary<string, string> StaticMaps { get; set; }

        /// <summary>
        /// Gets or sets the Twilio config for sending text message notifications
        /// </summary>
        [JsonProperty("twilio")]
        public TwilioConfig Twilio { get; set; }

        /// <summary>
        /// Gets or sets the Google maps key for location lookup
        /// </summary>
        [JsonProperty("gmapsKey")]
        public string GoogleMapsKey { get; set; }

        /// <summary>
        /// Gets or sets the OpenStreetMaps Nominatim endpoint to use for reverse location lookup
        /// </summary>
        [JsonProperty("nominatim")]
        public string NominatimEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the OpenStreetMaps Nominatim location string schema
        /// </summary>
        [JsonProperty("nominatimSchema")]
        public string NominatimSchema { get; set; }

        /// <summary>
        /// Gets or sets the minimum despawn time in minutes a Pokemon must have in order to send the alarm
        /// </summary>
        [JsonProperty("despawnTimeMinimumMinutes")]
        public int DespawnTimeMinimumMinutes { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes to reload subscriptions to accomodate the UI changes
        /// </summary>
        [JsonProperty("reloadSubscriptionChangesMinutes")]
        public ushort ReloadSubscriptionChangesMinutes { get; set; }

        /// <summary>
        /// Gets or sets the maximum amount of notifications a user can receive per minute per server before being rate limited
        /// </summary>
        [JsonProperty("maxNotificationsPerMinute")]
        public ushort MaxNotificationsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether to check for duplicate webhook messages or not
        /// </summary>
        [JsonProperty("checkForDuplicates")]
        public bool CheckForDuplicates { get; set; }

        /// <summary>
        /// Gets or sets whether to log incoming webhook data to a file
        /// </summary>
        [JsonProperty("debug")]
        public bool Debug { get; set; }

        [JsonProperty("maxPokemonId")]
        public uint MaxPokemonId { get; set; }

        /// <summary>
        /// Gets or sets the event logging level to set
        /// </summary>
        [JsonProperty("logLevel")]
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the configuration file path
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="WhConfig"/> class
        /// </summary>
        public WhConfig()
        {
            ListeningHost = "127.0.0.1";
            WebhookPort = 8008;
            Locale = "en";
            MaxPokemonId = 800;
            LogLevel = LogLevel.Trace;
            Servers = new Dictionary<ulong, DiscordServerConfig>();
            Database = new ConnectionStringsConfig();
            Urls = new UrlConfig();
            EventPokemonIds = new List<uint>();
            EventMinimumIV = 90;
            IconStyles = new Dictionary<string, string>();
            StaticMaps = new Dictionary<string, string>();
            Twilio = new TwilioConfig();
            DespawnTimeMinimumMinutes = 5;
            ReloadSubscriptionChangesMinutes = 1;
            MaxNotificationsPerMinute = 10;
            CheckForDuplicates = true;
        }

        /// <summary>
        /// Load Discords from the `/discords` folder
        /// </summary>
        /// <returns>Returns parsed alert message</returns>
        public void LoadDiscordServers()
        {
            if (!Directory.Exists(Strings.DiscordsFolder))
            {
                // Discords folder does not exist
                return;
            }

            var dict = new Dictionary<ulong, DiscordServerConfig>();
            foreach (var (guildId, fileName) in ServerConfigFiles)
            {
                var id = ulong.Parse(guildId);
                var path = Path.Combine(Strings.DiscordsFolder, fileName);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Discord server config file {path} not found.", path);
                }
                if (!dict.ContainsKey(id))
                {
                    var json = File.ReadAllText(path);
                    var config = JsonConvert.DeserializeObject<DiscordServerConfig>(json);
                    dict.Add(id, config);
                }
            }
            Servers = dict;
        }

        /// <summary>
        /// Save the current configuration object
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        public void Save(string filePath)
        {
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, data);
        }

        /// <summary>
        /// Load the configuration from a file
        /// </summary>
        /// <param name="filePath">Path to load the configuration file from</param>
        /// <returns>Returns the deserialized configuration object</returns>
        public static WhConfig Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Config not loaded because file not found.", filePath);
            }
            var config = MasterFile.LoadInit<WhConfig>(filePath);
            config.LoadDiscordServers();
            return config;
        }
    }
}

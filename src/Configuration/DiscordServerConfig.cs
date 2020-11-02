﻿namespace WhMgr.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Data;

    /// <summary>
    /// Discord server configuration class
    /// </summary>
    public class DiscordServerConfig
    {
        /// <summary>
        /// Gets or sets the command prefix for all Discord commands
        /// </summary>
        [JsonProperty("commandPrefix")]
        public string CommandPrefix { get; set; }

        /// <summary>
        /// Gets or sets the emoji guild id
        /// </summary>
        [JsonProperty("emojiGuildId")]
        public ulong EmojiGuildId { get; set; }

        /// <summary>
        /// Gets or sets the owner id
        /// </summary>
        [JsonProperty("ownerId")]
        public ulong OwnerId { get; set; }

        //[JsonProperty("locale")]
        //public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the donor role ID(s)
        /// </summary>
        [JsonProperty("donorRoleIds")]
        public List<ulong> DonorRoleIds { get; set; }

        /// <summary>
        /// Gets or sets the moderators of the Discord server
        /// </summary>
        [JsonProperty("moderatorIds")]
        public List<ulong> Moderators { get; set; }

        /// <summary>
        /// Gets or sets the Discord bot token
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the associated alarms file for the Discord server
        /// </summary>
        [JsonProperty("alarms")]
        public string AlarmsFile { get; set; }

        /// <summary>
        /// Gets or sets whether to enable custom direct message subscriptions
        /// </summary>
        [JsonProperty("enableSubscriptions")]
        public bool EnableSubscriptions { get; set; }

        /// <summary>
        /// Gets or sets whether to enable Discord city roles
        /// </summary>
        [JsonProperty("enableCities")]
        public bool EnableCities { get; set; }

        /// <summary>
        /// Gets or sets a list of named Discord roles associated with Geofence files
        /// </summary>
        [JsonProperty("cityRoles")]
        public List<string> CityRoles { get; set; }

        /// <summary>
        /// Gets or sets whether city roles require a Donor role
        /// </summary>
        [JsonProperty("citiesRequireSupporterRole")]
        public bool CitiesRequireSupporterRole { get; set; }

        /// <summary>
        /// Gets or sets whether to prune previous field research quest channels at midnight
        /// </summary>
        [JsonProperty("pruneQuestChannels")]
        public bool PruneQuestChannels { get; set; }

        /// <summary>
        /// Gets or sets a list of field research quest channel ID(s) to reset
        /// </summary>
        [JsonProperty("questChannelIds")]
        public List<ulong> QuestChannelIds { get; set; }

        /// <summary>
        /// Gets or sets the nests channel ID to report nests
        /// </summary>
        [JsonProperty("nestsChannelId")]
        public ulong NestsChannelId { get; set; }

        /// <summary>
        /// Gets or sets the minimum nest spawns per hour to limit nest posts by
        /// </summary>
        [JsonProperty("nestsMinimumPerHour")]
        public int NestsMinimumPerHour { get; set; }

        /// <summary>
        /// Gets or sets the shiny stats configuration class
        /// </summary>
        [JsonProperty("shinyStats")]
        public ShinyStatsConfig ShinyStats { get; set; }

        /// <summary>
        /// Gets or sets the icon style for messages on the Discord server
        /// </summary>
        [JsonProperty("iconStyle")]
        public string IconStyle { get; set; }

        /// <summary>
        /// Gets or sets the bot channel ID(s)
        /// </summary>
        [JsonProperty("botChannelIds")]
        public List<ulong> BotChannelIds { get; set; }

        /// <summary>
        /// Gets or sets the Discord bot's custom status
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("dmAlertsFile")]
        public string DmAlertsFile { get; set; }

        [JsonIgnore]
        public AlertMessage DmAlerts { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="DiscordServerConfig"/> class
        /// </summary>
        public DiscordServerConfig()
        {
            //Locale = "en";
            Moderators = new List<ulong>();
            CityRoles = new List<string>();
            IconStyle = "Default";
            QuestChannelIds = new List<ulong>();
            ShinyStats = new ShinyStatsConfig();
            NestsMinimumPerHour = 1;
            DmAlertsFile = "default.json";

            LoadDmAlerts();
        }

        public void LoadDmAlerts()
        {
            var path = Path.Combine(Strings.AlertsFolder, DmAlertsFile);
            DmAlerts = MasterFile.LoadInit<AlertMessage>(path);
        }
    }
}
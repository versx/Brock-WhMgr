﻿namespace WhMgr.Alarms.Models
{
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    using WhMgr.Alarms.Alerts;
    using WhMgr.Alarms.Filters.Models;
    using WhMgr.Geofence;

    /// <summary>
    /// Alarm filter class
    /// </summary>
    [JsonObject("alarm")]
    public class AlarmObject
    {
        /// <summary>
        /// Area geofences
        /// </summary>
        [JsonIgnore]
        public List<GeofenceItem> GeofenceItems { get; private set; }

        /// <summary>
        /// Discord alert messages
        /// </summary>
        [JsonIgnore]
        public AlertMessage Alerts { get; private set; }

        /// <summary>
        /// Alarm filters
        /// </summary>
        [JsonIgnore]
        public FilterObject Filters { get; private set; }

        /// <summary>
        /// Gets or sets the Alarm name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Discord message content outside of the embed message. (above it, can contain role/user mentions, DTS, etc)
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the filters file to load
        /// </summary>
        [JsonProperty("filters")]
        public string FiltersFile { get; set; }

        /// <summary>
        /// Gets or sets the alerts file to load
        /// </summary>
        [JsonProperty("alerts")]
        public string AlertsFile { get; set; }

        /// <summary>
        /// Gets or sets the geofences file to load
        /// </summary>
        [JsonProperty("geofences")]
        public List<string> Geofences { get; set; }

        /// <summary>
        /// Gets or sets the Discord channel webhook url address
        /// </summary>
        [JsonProperty("webhook")]
        public string Webhook { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="AlarmObject"/> class
        /// </summary>
        public AlarmObject()
        {
            GeofenceItems = new List<GeofenceItem>();
            LoadAlerts();
            LoadFilters();
        }

        /// <summary>
        /// Load alerts from the `/Alerts` folder
        /// </summary>
        /// <returns>Returns parsed alert message</returns>
        public AlertMessage LoadAlerts()
        {
            if (string.IsNullOrEmpty(AlertsFile))
                return null;

            var path = Path.Combine(Strings.AlertsFolder, AlertsFile);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Alert file {path} not found.", path);

            var data = File.ReadAllText(path);
            return Alerts = JsonConvert.DeserializeObject<AlertMessage>(data);
        }

        /// <summary>
        /// Load alarm filters from the `/Filters` folder
        /// </summary>
        /// <returns>Returns parsed filters object</returns>
        public FilterObject LoadFilters()
        {
            if (string.IsNullOrEmpty(FiltersFile))
                return null;

            var path = Path.Combine(Strings.FiltersFolder, FiltersFile);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Filter file {path} not found.", path);

            var data = File.ReadAllText(path);
            return Filters = JsonConvert.DeserializeObject<FilterObject>(data);
        }
    }
}
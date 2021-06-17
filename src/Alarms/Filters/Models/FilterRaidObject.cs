namespace WhMgr.Alarms.Filters.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using WhMgr.Net.Models;

    /// <summary>
    /// Raid boss filters
    /// </summary>
    public class FilterRaidObject
    {
        /// <summary>
        /// Gets or sets a value determining whether to enable the raid boss filter
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the minimum raid level
        /// </summary>
        [JsonProperty("min_lvl")]
        public uint MinimumLevel { get; set; }

        /// <summary>
        /// Gets or sets the maximum raid level
        /// </summary>
        [JsonProperty("max_lvl")]
        public uint MaximumLevel { get; set; }

        /// <summary>
        /// Gets or sets the Raid boss pokedex ID list to filter against
        /// </summary>
        //TODO: Allow pokemon names and ids for raid filter.
        [JsonProperty("pokemon")]
        public List<uint> Pokemon { get; set; }

        /// <summary>
        /// Gets or sets the list of Raid Boss Pokemon Form strings to filter against
        /// </summary>
        [JsonProperty("forms")]
        public List<string> Forms { get; set; }

        /// <summary>
        /// Gets or sets the list of Raid Boss Pokemon costume strings to filter against
        /// </summary>
        [JsonProperty("costumes")]
        public List<string> Costumes { get; set; }

        /// <summary>
        /// Gets or sets the Raid boss filter type
        /// </summary>
        [JsonProperty("type")]
        public FilterType FilterType { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether to only report ex-eligible raids
        /// </summary>
        [JsonProperty("onlyEx")]
        public bool OnlyEx { get; set; }

        /// <summary>
        /// Gets or sets the Gym team control filter
        /// </summary>
        [JsonProperty("team")]
        public PokemonTeam Team { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether to ignore raids missing stats
        /// </summary>
        [JsonProperty("ignoreMissing")]
        public bool IgnoreMissing { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="FilterRaidObject"/> class
        /// </summary>
        public FilterRaidObject()
        {
            Pokemon = new List<uint>();
            Forms = new List<string>();
            Costumes = new List<string>();
            MinimumLevel = 1;
            MaximumLevel = 5;
            Team = PokemonTeam.All;
        }
    }
}
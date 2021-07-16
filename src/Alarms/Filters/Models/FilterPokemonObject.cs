namespace WhMgr.Alarms.Filters.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using WhMgr.Net.Models;

    /// <summary>
    /// Pokemon filters
    /// </summary>
    public class FilterPokemonObject
    {
        /// <summary>
        /// Gets or sets a value determining whether to enable the pokemon filter
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the list of pokemon pokedex IDs to filter against
        /// </summary>
        //TODO: Allow pokemon names and ids for pokemon filter.
        [JsonProperty("pokemon")]
        public List<uint> Pokemon { get; set; }

        /// <summary>
        /// Gets or sets the list of pokemon Form strings to filter against
        /// </summary>
        [JsonProperty("forms")]
        public List<string> Forms { get; set; }

        /// <summary>
        /// Gets or sets the list of Pokemon costume strings to filter against
        /// </summary>
        [JsonProperty("costumes")]
        public List<string> Costumes { get; set; }

        /// <summary>
        /// Gets or sets the minimum IV value to report
        /// </summary>
        [JsonProperty("min_iv")]
        public uint MinimumIV { get; set; }

        /// <summary>
        /// Gets or sets the maximum IV value to report
        /// </summary>
        [JsonProperty("max_iv")]
        public uint MaximumIV { get; set; }

        /// <summary>
        /// Gets or sets the minimum CP value to report
        /// </summary>
        [JsonProperty("min_cp")]
        public uint MinimumCP { get; set; }

        /// <summary>
        /// Gets or sets the maximum CP value to report
        /// </summary>
        [JsonProperty("max_cp")]
        public uint MaximumCP { get; set; }

        /// <summary>
        /// Gets or sets the minimum level value to report
        /// </summary>
        [JsonProperty("min_lvl")]
        public uint MinimumLevel { get; set; }

        /// <summary>
        /// Gets or sets the maximum level value to report
        /// </summary>
        [JsonProperty("max_lvl")]
        public uint MaximumLevel { get; set; }

        /// <summary>
        /// Gets or sets the Pokemon gender to filter by
        /// </summary>
        [JsonProperty("gender")]
        public char Gender { get; set; }

        /// <summary>
        /// Gets or sets the Pokemon size to filter by
        /// </summary>
        [JsonProperty("size")]
        public PokemonSize? Size { get; set; }

        /// <summary>
        /// Gets or sets a value determining to filter only great league PvP eligible Pokemon
        /// </summary>
        [JsonProperty("great_league")]
        public bool IsPvpGreatLeague { get; set; }

        /// <summary>
        /// Gets or sets a value determining to filter only ultra league PvP eligible Pokemon
        /// </summary>
        [JsonProperty("ultra_league")]
        public bool IsPvpUltraLeague { get; set; }

        /// <summary>
        /// Gets or sets the minimum PvP rank to report
        /// </summary>
        [JsonProperty("min_rank")]
        public uint MinimumRank { get; set; }

        /// <summary>
        /// Gets or sets the maximum PvP rank to report
        /// </summary>
        [JsonProperty("max_rank")]
        public uint MaximumRank { get; set; }

        //TODO: Filter by move?

        /// <summary>
        /// Gets or sets the Pokemon filter type
        /// </summary>
        [JsonProperty("type")]
        public FilterType FilterType { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether a Pokemon was checked with an event account
        /// </summary>
        [JsonProperty("is_event")]
        public bool IsEvent { get; set; }

        /// <summary>
        /// Gets or sets a value determining whether to ignore Pokemon missing stats
        /// </summary>
        [JsonProperty("ignoreMissing")]
        public bool IgnoreMissing { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="FilterPokemonObject"/> class.
        /// </summary>
        public FilterPokemonObject()
        {
            Pokemon = new List<uint>();
            Forms = new List<string>();
            Costumes = new List<string>();
            MinimumIV = 0;
            MaximumIV = 100;
            MinimumCP = 0;
            MaximumCP = 999999;
            MinimumLevel = 0;
            MaximumLevel = 100; //Support for when they increase level cap. :wink:
            MinimumRank = 0;
            MaximumRank = 4096;
            Gender = '*';
            Size = null;
        }
    }
}
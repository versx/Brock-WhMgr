﻿namespace WhMgr.Data.Subscriptions.Models
{
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;
    using ServiceStack.DataAnnotations;

    using WhMgr.Net.Models;

    [
        JsonObject("pokemon"),
        Alias("pokemon"),
    ]
    public class PokemonSubscription : SubscriptionItem
    {
        #region Properties

        [
            Alias("subscription_id"),
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonProperty("pokemon_id"),
            Alias("pokemon_id"),
            Required
        ]
        public int PokemonId { get; set; }

        [
            JsonProperty("form"),
            Alias("form"),
        ]
        public string Form { get; set; }

        [
            JsonProperty("min_cp"),
            Alias("min_cp"),
        ]
        public int MinimumCP { get; set; }

        [
            JsonProperty("min_iv"),
            Alias("min_iv"),
        ]
        public int MinimumIV { get; set; }

        [
            JsonProperty("iv_list"),
            Alias("iv_list"),
        ]
        public List<string> IVList { get; set; }

        [
            JsonProperty("min_lvl"),
            Alias("min_lvl"),
        ]
        public int MinimumLevel { get; set; }

        [
            JsonProperty("max_lvl"),
            Alias("max_lvl"),
        ]
        public int MaximumLevel { get; set; }

        [
            JsonProperty("gender"),
            Alias("gender"),
        ]
        public string Gender { get; set; }

        [
            JsonProperty("size"),
            Alias("size"),
            Default(0),
        ]
        public PokemonSize Size { get; set; }

        [
            JsonProperty("city"),
            Alias("city"),
        ]
        public List<string> Areas { get; set; }

        [
            JsonProperty("location"),
            Alias("location"),
        ]
        public string Location { get; set; }

        [
            JsonIgnore,
            Ignore
        ]
        public bool HasStats => IVList?.Any() ?? false;

        #endregion

        #region Constructor

        public PokemonSubscription()
        {
            MinimumCP = 0;
            MinimumIV = 0;
            MinimumLevel = 0;
            MaximumLevel = 35;
            Gender = "*";
            Size = PokemonSize.All;
            Form = null;
            Areas = new List<string>();
            IVList = new List<string>();
        }

        #endregion
    }
}
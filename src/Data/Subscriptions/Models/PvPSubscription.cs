namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using ServiceStack.DataAnnotations;

    [
        JsonObject("pvp"),
        Alias("pvp"),
    ]
    public class PvPSubscription : SubscriptionItem
    {
        [
            Alias("subscription_id"),
            ForeignKey(typeof(SubscriptionObject))
        ]
        public int SubscriptionId { get; set; }

        [
            JsonProperty("pokemon_id"),
            Alias("pokemon_id"),
            Required
        ]
        public uint PokemonId { get; set; }

        [
            JsonProperty("form"),
            Alias("form")
        ]
        public string Form { get; set; }

        [
            JsonProperty("league"),
            Alias("league"),
            Required
        ]
        public PvPLeague League { get; set; }

        [
            JsonProperty("min_rank"),
            Alias("min_rank"),
            Default(25)
        ]
        public int MinimumRank { get; set; }

        [
            JsonProperty("min_percent"),
            Alias("min_percent"),
            Default(90.0)
        ]
        public double MinimumPercent { get; set; }

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

        public PvPSubscription()
        {
            Form = null;
            League = PvPLeague.Great;
            MinimumRank = 25;
            MinimumPercent = 90;
            Areas = new List<string>();
        }
    }
}
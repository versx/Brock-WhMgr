namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using ServiceStack.DataAnnotations;

    [
        JsonObject("raids"),
        Alias("raids"),
    ]
    public class RaidSubscription : SubscriptionItem
    {
        [
            Alias("subscription_id"),
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonProperty("pokemon_id"),
            Alias("pokemon_id"), 
            Required,
        ]
        public uint PokemonId { get; set; }

        [
            JsonProperty("form"),
            Alias("form"),
        ]
        public string Form { get; set; }

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

        public RaidSubscription()
        {
            Areas = new List<string>();
        }
    }
}
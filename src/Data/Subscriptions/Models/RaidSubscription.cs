namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using ServiceStack.DataAnnotations;

    [
        //JsonPropertyName("raids"),
        Alias("raids"),
    ]
    public class RaidSubscription : SubscriptionItem
    {
        [
            JsonPropertyName("subscription_id"),
            Alias("subscription_id"),
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonPropertyName("pokemon_id"),
            Alias("pokemon_id"), 
            Required,
        ]
        public uint PokemonId { get; set; }

        [
            JsonPropertyName("form"),
            Alias("form"),
        ]
        public string Form { get; set; }

        [
            JsonPropertyName("city"),
            Alias("city"),
        ]
        public List<string> Areas { get; set; }

        [
            JsonPropertyName("location"),
            Alias("location"),
        ]
        public string Location { get; set; }

        public RaidSubscription()
        {
            Areas = new List<string>();
        }
    }
}
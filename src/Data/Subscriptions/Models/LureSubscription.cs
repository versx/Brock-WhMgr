namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using ServiceStack.DataAnnotations;

    using WhMgr.Net.Models;

    [
        //JsonPropertyName("lures"),
        Alias("lures"),
    ]
    public class LureSubscription : SubscriptionItem
    {
        [
            JsonPropertyName("subscription_id"),
            Alias("subscription_id"),
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonPropertyName("pokestop_name"),
            Alias("pokestop_name"),
        ]
        public string PokestopName { get; set; }

        [
            JsonPropertyName("lure_type"),
            Alias("lure_type"),
            Required
        ]
        public PokestopLureType LureType { get; set; }

        [
            JsonPropertyName("city"),
            Alias("city"),
            Required
        ]
        public List<string> Areas { get; set; }

        [
            JsonPropertyName("location"),
            Alias("location"),
        ]
        public string Location { get; set; }

        public LureSubscription()
        {
            LureType = PokestopLureType.None;
            Areas = new List<string>();
        }
    }
}
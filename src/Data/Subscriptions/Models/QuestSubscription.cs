namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using ServiceStack.DataAnnotations;

    [
        //JsonPropertyName("quests"),
        Alias("quests"),
    ]
    public class QuestSubscription : SubscriptionItem
    {
        [
            JsonPropertyName("subscription_id"),
            Alias("subscription_id"), 
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonProperty("pokestop_name"),
            Alias("pokestop_name"),
        ]
        public string PokestopName { get; set; }

        [
            JsonPropertyName("reward"),
            Alias("reward"), 
            Required,
        ]
        public string RewardKeyword { get; set; }

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

        public QuestSubscription()
        {
            Areas = new List<string>();
        }
    }
}
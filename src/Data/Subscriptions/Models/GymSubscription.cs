namespace WhMgr.Data.Subscriptions.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using ServiceStack.DataAnnotations;

    [
        //JsonPropertyName("gyms"),
        Alias("gyms"),
    ]
    public class GymSubscription : SubscriptionItem
    {
        [
            JsonPropertyName("subscription_id"),
            Alias("subscription_id"),
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonPropertyName("name"),
            Alias("name"),
            //Unique,
        ]
        public string Name { get; set; }

        [
            JsonPropertyName("min_level"),
            Alias("min_level"),
        ]
        public ushort MinimumLevel { get; set; }

        [
            JsonPropertyName("max_level"),
            Alias("max_level"),
        ]
        public ushort MaximumLevel { get; set; }

        [
            JsonPropertyName("pokemon_ids"),
            Alias("pokemon_ids"),
        ]
        public List<uint> PokemonIDs { get; set; }

        [
            JsonPropertyName("location"),
            Alias("location"),
        ]
        public string Location { get; set; }

        public GymSubscription()
        {
            PokemonIDs = new List<uint>();
        }
    }
}
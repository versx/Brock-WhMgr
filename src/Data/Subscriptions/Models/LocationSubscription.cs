namespace WhMgr.Data.Subscriptions.Models
{
    using System.Text.Json.Serialization;
    using ServiceStack.DataAnnotations;

    [
        //JsonPropertyName("locations"),
        Alias("locations"),
    ]
    public class LocationSubscription : SubscriptionItem
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
        ]
        public string Name { get; set; }

        [
            JsonPropertyName("distance"),
            Alias("distance"),
            Default(0),
        ]
        public int DistanceM { get; set; }

        /// <summary>
        /// Gets or sets the latitude to use with distance checks
        /// </summary>
        [
            JsonPropertyName("latitude"),
            Alias("latitude"),
            Default(0),
        ]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude to use with distance checks
        /// </summary>
        [
            JsonPropertyName("longitude"),
            Alias("longitude"),
            Default(0),
        ]
        public double Longitude { get; set; }
    }
}
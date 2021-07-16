namespace WhMgr.Data.Subscriptions.Models
{
    using Newtonsoft.Json;
    using ServiceStack.DataAnnotations;

    [
        JsonObject("locations"),
        Alias("locations"),
    ]
    public class LocationSubscription : SubscriptionItem
    {
        [
            Alias("subscription_id"),
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonProperty("name"),
            Alias("name"),
        ]
        public string Name { get; set; }

        [
            JsonProperty("distance"),
            Alias("distance"),
            Default(0),
        ]
        public int DistanceM { get; set; }

        /// <summary>
        /// Gets or sets the latitude to use with distance checks
        /// </summary>
        [
            JsonProperty("latitude"),
            Alias("latitude"),
            Default(0),
        ]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude to use with distance checks
        /// </summary>
        [
            JsonProperty("longitude"),
            Alias("longitude"),
            Default(0),
        ]
        public double Longitude { get; set; }
    }
}
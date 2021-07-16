namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using ServiceStack.DataAnnotations;

    [Flags]
    public enum NotificationStatusType : byte
    {
        None = 0x0,
        Pokemon = 0x1,
        PvP = 0x2,
        Raids = 0x4,
        Quests = 0x8,
        Invasions = 0x10,
        Lures = 0x20,
        Gyms = 0x40,
        All = Pokemon | PvP | Raids | Quests | Invasions | Lures | Gyms,
    }

    /// <summary>
    /// User subscription class
    /// </summary>
    [
        //JsonPropertyName("subscriptions"),
        Alias("subscriptions")
    ]
    public class SubscriptionObject : SubscriptionItem
    {
        /// <summary>
        /// Gets or sets a value determining whether the associated users
        /// subscriptions are enabled or not
        /// </summary>
        [
            JsonPropertyName("status"),
            Alias("status"),
            Default((int)NotificationStatusType.All),
        ]
        public NotificationStatusType Status { get; set; }

        public bool IsEnabled(NotificationStatusType status)
        {
            return (Status & status) == status;
        }

        public void EnableNotificationType(NotificationStatusType status)
        {
            Status |= status;
        }

        public void DisableNotificationType(NotificationStatusType status)
        {
            Status &= (~status);
        }

        /// <summary>
        /// Gets or sets the Pokemon subscriptions
        /// </summary>
        [
            JsonPropertyName("pokemon"),
            Alias("pokemon"), 
            Reference,
        ]
        public List<PokemonSubscription> Pokemon { get; set; }

        /// <summary>
        /// Gets or sets the PvP Pokemon subscriptions
        /// </summary>
        [
            JsonPropertyName("pvp"),
            Alias("pvp"),
            Reference,
        ]
        public List<PvPSubscription> PvP { get; set; }

        /// <summary>
        /// Gets or sets the Raid subscriptions
        /// </summary>
        [
            JsonPropertyName("raids"),
            Alias("raids"), 
            Reference,
        ]
        public List<RaidSubscription> Raids { get; set; }

        /// <summary>
        /// Gets or sets the Gym subscriptions to use with Raid subscriptions
        /// </summary>
        [
            JsonPropertyName("gyms"),
            Alias("gyms"),
            Reference,
        ]
        public List<GymSubscription> Gyms { get; set; }

        /// <summary>
        /// Gets or sets the Quest subscriptions
        /// </summary>
        [
            JsonPropertyName("quests"),
            Alias("quests"),
            Reference,
        ]
        public List<QuestSubscription> Quests { get; set; }

        /// <summary>
        /// Gets or sets the Team Rocket Invasion subscriptions
        /// </summary>
        [
            JsonPropertyName("invasions"),
            Alias("invasions"),
            Reference,
        ]
        public List<InvasionSubscription> Invasions { get; set; }

        /// <summary>
        /// Gets or sets the distance in meters a subscription should be within
        /// to trigger
        /// </summary>
        [
            JsonPropertyName("lures"),
            Alias("lures"),
            Reference,
        ]
        public List<LureSubscription> Lures { get; set; }

        [
            JsonPropertyName("locations"),
            Alias("locations"),
            Reference,
        ]
        public List<LocationSubscription> Locations { get; set; }

        [
            JsonPropertyName("location"),
            Alias("location"),
            Default(null),
        ]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the icon style to use for the subscription notification
        /// </summary>
        [
            JsonPropertyName("icon_style"),
            Alias("icon_style"),
            Default("Default"),
        ]
        public string IconStyle { get; set; }

        /// <summary>
        /// Gets or sets the phone number to send ultra rare Pokemon notifications to
        /// </summary>
        [
            JsonPropertyName("phone_number"),
            Alias("phone_number"),
        ]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets the <seealso cref="NotificationLimiter"/> class associated with the subscription
        /// </summary>
        [
            JsonIgnore,
            Ignore,
        ]
        public NotificationLimiter Limiter { get; }

        /// <summary>
        /// Gets or sets a value determining whether the rate limit notification
        /// has been sent to the user already
        /// </summary>
        [
            JsonIgnore,
            Ignore,
        ]
        public bool RateLimitNotificationSent { get; set; }

        /// <summary>
        /// Instantiates a new subscription object
        /// </summary>
        public SubscriptionObject()
        {
            Status = NotificationStatusType.All;
            Pokemon = new List<PokemonSubscription>();
            PvP = new List<PvPSubscription>();
            Raids = new List<RaidSubscription>();
            Gyms = new List<GymSubscription>();
            Quests = new List<QuestSubscription>();
            Invasions = new List<InvasionSubscription>();
            Lures = new List<LureSubscription>();
            Locations = new List<LocationSubscription>();
            Limiter = new NotificationLimiter();
            IconStyle = "Default";
            PhoneNumber = string.Empty;
        }
    }
}
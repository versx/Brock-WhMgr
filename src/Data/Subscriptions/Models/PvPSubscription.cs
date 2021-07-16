namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            ForeignKey(typeof(SubscriptionObject)),
        ]
        public int SubscriptionId { get; set; }

        [
            JsonIgnore,
            Ignore,
        ]
        public List<uint> PokemonId
        {
            get
            {
                try
                {
                    return PokemonIdString?.Split(',')?
                                           .Select(x => uint.Parse(x))
                                           .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Failed to parse pokemon id string: {ex}");
                }
                return new List<uint>();
            }
        }

        [
            JsonProperty("pokemon_id"),
            Alias("pokemon_id"),
            Required,
        ]
        public string PokemonIdString { get; set; }

        [
            JsonIgnore,
            Ignore,
        ]
        public List<string> Forms => FormsString?.Split(',').ToList();

        [
            JsonProperty("form"),
            Alias("form"),
            Default(null),
        ]
        public string FormsString { get; set; }

        [
            JsonProperty("league"),
            Alias("league"),
            Required,
        ]
        public PvPLeague League { get; set; }

        [
            JsonProperty("min_rank"),
            Alias("min_rank"),
            Default(25),
        ]
        public int MinimumRank { get; set; }

        [
            JsonProperty("min_percent"),
            Alias("min_percent"),
            Default(90.0),
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
            League = PvPLeague.Great;
            MinimumRank = 25;
            MinimumPercent = 90;
            Areas = new List<string>();
        }
    }
}
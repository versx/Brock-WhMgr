namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
    using Newtonsoft.Json;
    using ServiceStack.DataAnnotations;

    [
        JsonObject("invasions"),
        Alias("invasions"),
    ]
    public class InvasionSubscription : SubscriptionItem
    {
        [
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
            JsonProperty("grunt_type"),
            Alias("grunt_type"),
        ]
        public InvasionCharacter InvasionType { get; set; }

        [
            JsonIgnore,
            Ignore,
        ]
        public List<uint> RewardPokemonId => RewardPokemonIdString?.Split(',')?
                                                                   .Select(x => uint.Parse(x))
                                                                   .ToList();

        [
            JsonProperty("reward_pokemon_id"),
            Alias("reward_pokemon_id"),
        ]
        public string RewardPokemonIdString { get; set; }

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

        public InvasionSubscription()
        {
            Areas = new List<string>();
        }
    }
}
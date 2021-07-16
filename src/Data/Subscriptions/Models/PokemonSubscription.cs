namespace WhMgr.Data.Subscriptions.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    using ServiceStack.DataAnnotations;

    using WhMgr.Net.Models;

    [
        //JsonPropertyName("pokemon"),
        Alias("pokemon"),
    ]
    public class PokemonSubscription : SubscriptionItem
    {
        #region Properties

        [
            JsonPropertyName("subscription_id"),
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
                    return PokemonIdString?.TrimEnd(',')
                                           .Split(',')?
                                           .Select(x => uint.Parse(x))
                                           .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Failed to parse pokemon id string: {PokemonIdString}\nSubscriptionId: {SubscriptionId}\nPokemonSubscriptionId: {Id}\nError: {ex}");
                }
                return new List<uint>();
            }
        }

        [
            JsonPropertyName("pokemon_id"),
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
            JsonPropertyName("form"),
            Alias("form"),
        ]
        public string FormsString { get; set; }

        [
            JsonPropertyName("min_cp"),
            Alias("min_cp"),
        ]
        public int MinimumCP { get; set; }

        [
            JsonPropertyName("min_iv"),
            Alias("min_iv"),
        ]
        public int MinimumIV { get; set; }

        [
            JsonPropertyName("iv_list"),
            Alias("iv_list"),
        ]
        public List<string> IVList { get; set; }

        [
            JsonPropertyName("min_lvl"),
            Alias("min_lvl"),
        ]
        public int MinimumLevel { get; set; }

        [
            JsonPropertyName("max_lvl"),
            Alias("max_lvl"),
        ]
        public int MaximumLevel { get; set; }

        [
            JsonPropertyName("gender"),
            Alias("gender"),
        ]
        public string Gender { get; set; }

        [
            JsonIgnore,
            Ignore,
        ]
        public PokemonSize Size => (PokemonSize)_Size;

        [
            JsonProperty("size"),
            Alias("size"),
            Default((uint)PokemonSize.All),
        ]
        public uint _Size { get; set; }

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

        [
            JsonIgnore,
            Ignore
        ]
        public bool HasStats => IVList?.Any() ?? false;

        #endregion

        #region Constructor

        public PokemonSubscription()
        {
            MinimumCP = 0;
            MinimumIV = 0;
            MinimumLevel = 0;
            MaximumLevel = 35;
            Gender = "*";
            _Size = (uint)PokemonSize.All;
            FormsString = null;
            Areas = new List<string>();
            IVList = new List<string>();
        }

        #endregion
    }
}
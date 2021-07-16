namespace WhMgr.Data.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using POGOProtos.Rpc;

    public class PokedexPokemon
    {
        [JsonProperty("pokedex_id")]
        public uint PokedexId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("forms")]
        public Dictionary<int, PokedexPokemon> Forms { get; set; }

        [JsonProperty("default_form_id")]
        public int? DefaultFormId { get; set; }

        [JsonProperty("default_form")]
        public string DefaultForm { get; set; }

        [JsonProperty("evolutions")]
        public List<PokedexPokemon> Evolutions { get; set; }

        [JsonProperty("form")]
        public string Form { get; set; }

        [JsonProperty("types")]
        public List<PokemonType> Types { get; set; }

        [JsonProperty("attack")]
        public int? Attack { get; set; }

        [JsonProperty("defense")]
        public int? Defense { get; set; }

        [JsonProperty("stamina")]
        public int? Stamina { get; set; }

        [JsonProperty("height")]
        public double? Height { get; set; }

        [JsonProperty("weight")]
        public double? Weight { get; set; }

        [JsonProperty("flee_rate")]
        public double? FleeRate { get; set; }

        [JsonProperty("quick_moves")]
        public List<string> QuickMoves { get; set; }

        [JsonProperty("charged_moves")]
        public List<string> ChargedMoves { get; set; }

        [JsonProperty("candy_to_evolve")]
        public int? Candy { get; set; }

        [JsonProperty("buddy_distance")]
        public int? BuddyDistance { get; set; }

        [JsonProperty("legendary")]
        public bool Legendary { get; set; }

        [JsonProperty("mythic")]
        public bool Mythical { get; set; }

        [JsonProperty("buddy_distance_evolve")]
        public int? BuddyDistanceEvolve { get; set; }

        [JsonProperty("third_move_stardust")]
        public int ThirdMoveStardust { get; set; }

        [JsonProperty("third_move_candy")]
        public int ThirdMoveCandy { get; set; }

        [JsonProperty("gym_defender_eligible")]
        public bool GymDeployable { get; set; }

        public PokedexPokemon()
        {
            Forms = new Dictionary<int, PokedexPokemon>();
            Evolutions = new List<PokedexPokemon>();
            QuickMoves = new List<string>();
            ChargedMoves = new List<string>();
            Types = new List<PokemonType>();
        }
    }
}
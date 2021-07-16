namespace WhMgr.Data.Models
{
    using System;

    using ServiceStack.DataAnnotations;

    [Alias("nests")]
    public class Nest
    {
        [Alias("name")]
        public string Name { get; set; }

        [Alias("pokemon_avg")]
        public int Average { get; set; }

        [Alias("pokemon_count")]
        public int Count { get; set; }

        [Alias("pokemon_id")]
        public uint PokemonId { get; set; }

        [Alias("lat")]
        public double Latitude { get; set; }

        [Alias("lon")]
        public double Longitude { get; set; }

        [Alias("updated")]
        public DateTime LastUpdated { get; set; }
    }
}
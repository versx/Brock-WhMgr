namespace WhMgr.Net.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// PVP Pokemon rank class.
    /// </summary>
    public sealed class PVPRank
    {
        [JsonProperty("rank")]
        public int? Rank { get; set; }

        [JsonProperty("percentage")]
        public double? Percentage { get; set; }

        [JsonProperty("pokemon")]
        public uint PokemonId { get; set; }

        [JsonProperty("form")]
        public int FormId { get; set; }

        [JsonProperty("level")]
        public double? Level { get; set; }

        [JsonProperty("cp")]
        public int? CP { get; set; }
    }
}
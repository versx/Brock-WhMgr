namespace WhMgr.Configuration
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class TwilioConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("accountSid")]
        public string AccountSid { get; set; }

        [JsonProperty("authToken")]
        public string AuthToken { get; set; }

        [JsonProperty("from")]
        public string FromNumber { get; set; }

        [JsonProperty("userIds")]
        public List<ulong> UserIds { get; set; }

        [JsonProperty("roleIds")]
        public List<ulong> RoleIds { get; set; }

        [JsonProperty("pokemonIds")]
        public List<uint> PokemonIds { get; set; }

        [JsonProperty("minIV")]
        public int MinimumIV { get; set; }

        public TwilioConfig()
        {
            UserIds = new List<ulong>();
            PokemonIds = new List<uint>
            {
                201, // Unown
                480, // Uxie
                481, // Mesprite
                482, // Azelf
                443, // Gible
                444, // Gabite
                445, // Garchomp
                633, // Deino
                634, // Zweilous
                635, // Hydreigon
                610, // Axew
                611, // Fraxure
                612 // Haxorus
            };
            MinimumIV = 100;
        }
    }
}
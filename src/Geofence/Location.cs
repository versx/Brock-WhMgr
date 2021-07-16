namespace WhMgr.Geofence
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;

    using NetTopologySuite.Geometries;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using SmartFormat;

    using WhMgr.Configuration;
    using WhMgr.Diagnostics;

    /// <summary>
    /// NominatimReverseLookup class
    /// </summary>
    public partial class NominatimReverseLookup
    {
        [JsonProperty("place_id")]
        public long PlaceId { get; set; }

        [JsonProperty("licence")]
        public string Licence { get; set; }

        [JsonProperty("osm_type")]
        public string OsmType { get; set; }

        [JsonProperty("osm_id")]
        public long OsmId { get; set; }

        [JsonProperty("lat")]
        public decimal Lat { get; set; }

        [JsonProperty("lon")]
        public decimal Lon { get; set; }

        [JsonProperty("place_rank")]
        public long PlaceRank { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("importance")]
        public long Importance { get; set; }

        [JsonProperty("addresstype")]
        public string Addresstype { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("address")]
        public NominatimAddress Address { get; set; }

        [JsonProperty("boundingbox")]
        public decimal[] Boundingbox { get; set; }
    }

    /// <summary>
    /// NominatimAddress class
    /// </summary>
    public partial class NominatimAddress
    {
        [JsonProperty("house_number")]
        public string HouseNumber { get; set; }

        [JsonProperty("road")]
        public string Road { get; set; }

        [JsonProperty("neighbourhood")]
        public string Neighbourhood { get; set; }

        [JsonProperty("suburb")]
        public string Suburb { get; set; }

        [JsonProperty("town")]
        private string Town { set { City = value; } }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }

    public class LocationCache : Dictionary<(double, double), Location>
    {
    }

    /// <summary>
    /// Geocoordinate location
    /// </summary>
    public class Location
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("LOCATION", Program.LogLevel);
        private static readonly LocationCache _cache = new LocationCache();

        /// <summary>
        /// Gets or sets the address for the location
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Gets the city of the address
        /// </summary>
        public string City { get; }

        /// <summary>
        /// Gets the geocoordinate latitude
        /// </summary>
        public double Latitude { get; }

        /// <summary>
        /// Gets the geocoordinate longitude
        /// </summary>
        public double Longitude { get; }

        /// <summary>
        /// Instantiates a new <see cref="Location"/> class
        /// </summary>
        /// <param name="lat">Geocoordinate latitude</param>
        /// <param name="lng">Geocoordinate longitude</param>
        public Location(double lat, double lng)
        {
            Latitude = lat;
            Longitude = lng;
        }

        /// <summary>
        /// Instantiates a new <see cref="Location"/> class
        /// </summary>
        /// <param name="address">Address of geocoordinates</param>
        /// <param name="city">City of address</param>
        /// <param name="lat">Geocoordinate latitude</param>
        /// <param name="lng">Geocoordinate longitude</param>
        public Location(string address, string city, double lat, double lng)
        {
            Address = address;
            City = city;
            Latitude = lat;
            Longitude = lng;
        }

        /// <summary>
        /// Gets the geolocation lookup of the current <seealso cref="Location"/> object.
        /// </summary>
        /// <param name="config">Config that includes the Google Maps key and OSM Nominatim endpoint</param>
        /// <returns>Returns a <seealso cref="Location"/> object containing the address</returns>
        public Location GetAddress(WhConfig config)
        {
            var key = (Latitude, Longitude);
            // Check if cache already contains lat/lon tuple key, if so return it.
            if (_cache.ContainsKey(key))
            {
                return _cache[key];
            }

            // Check if we want any reverse geocoding address
            Location location = null;
            if (!string.IsNullOrEmpty(config.GoogleMapsKey))
            {
                location = GetGoogleAddress(City, Latitude, Longitude, config.GoogleMapsKey);
            }

            if (!string.IsNullOrEmpty(config.NominatimEndpoint))
            {
                location = GetNominatimAddress(City, Latitude, Longitude, config.NominatimEndpoint, config.NominatimSchema);
            }

            // Check if lat/lon tuple key has not been cached already, if not add it.
            if (!_cache.ContainsKey(key))
            {
                _cache.Add(key, location);
            }
            return location;
        }

        /// <summary>
        /// Returns the string representation of <see cref="Location"/> class
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Latitude}, {Longitude}";
        }

        public static implicit operator Coordinate(Location location) => new Coordinate(location.Longitude, location.Latitude);

        public static implicit operator Point(Location location) => new Point(location.Longitude, location.Latitude);

        /// <summary>
        /// Queries Google Maps geolocation lookup endpoint
        /// </summary>
        /// <param name="city">Geofence specific city to associate with the returned address</param>
        /// <param name="lat">Latitude to lookup</param>
        /// <param name="lng">Longitude to lookup</param>
        /// <param name="gmapsKey">Google Maps key</param>
        /// <returns></returns>
        private Location GetGoogleAddress(string city, double lat, double lng, string gmapsKey)
        {
            var apiKey = string.IsNullOrEmpty(gmapsKey) ? string.Empty : $"&key={gmapsKey}";
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&sensor=true{apiKey}";
            var unknown = "Unknown";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream, Encoding.UTF8);
                    var data = reader.ReadToEnd();
                    var parseJson = JObject.Parse(data);
                    var status = Convert.ToString(parseJson["status"]);
                    if (string.Compare(status, "OK", true) != 0)
                        return null;

                    var result = parseJson["results"].FirstOrDefault();
                    var address = Convert.ToString(result["formatted_address"]);
                    //var area = Convert.ToString(result["address_components"][2]["long_name"]);
                    return new Location(address, city ?? unknown, lat, lng);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return null;
        }

        /// <summary>
        /// Queries OpenStreetMaps Nominatim geolocation lookup endpoint
        /// </summary>
        /// <param name="city">Geofence specific city to associate with the returned address</param>
        /// <param name="lat">Latitude to lookup</param>
        /// <param name="lng">Longitude to lookup</param>
        /// <param name="endpoint">Nominatim endpoint</param>
        /// <param name="nominatimSchema">Nominatim schema</param>
        /// <returns></returns>
        private Location GetNominatimAddress(string city, double lat, double lng, string endpoint, string nominatimSchema)
        {
            var unknown = "Unknown";
            var url = $"{endpoint}/reverse?format=jsonv2&lat={lat}&lon={lng}";
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Proxy = null;
                    wc.Headers.Add("User-Agent", Strings.BotName);
                    var json = wc.DownloadString(url);
                    dynamic obj = JsonConvert.DeserializeObject<NominatimReverseLookup>(json);
                    var location_string = Smart.Format(nominatimSchema, obj);
                    return new Location(location_string, city ?? unknown, Convert.ToDouble(obj.Lat), Convert.ToDouble(obj.Lon));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return null;
        }
    }
}
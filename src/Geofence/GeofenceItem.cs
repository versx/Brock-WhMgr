namespace WhMgr.Geofence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using NetTopologySuite.Features;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.IO;
    using Newtonsoft.Json;

    using WhMgr.Utilities;

    /// <summary>
    /// Geofence class
    /// </summary>
    public class GeofenceItem
    {
        private const string DefaultName = "Unnamed";

        #region Properties

        /// <summary>
        /// Gets or sets the name of the geofence
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The filename from which this geofence originated
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets the FeatureCollection containing the geometry which represents this geofence
        /// </summary>
        public IFeature Feature { get; }
        
        /// <summary>
        /// Gets the geometry representing the smallest possible bounding box which contains all points of this geofence
        /// </summary>
        public Geometry BBox { get; }

        /// <summary>
        /// Gets or sets the priority of this geofence. Higher-priority geofences will take precedence
        /// when determining which geofence a particular location falls within if it falls within multiple.
        /// </summary>
        public int Priority { get; set; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Instantiates a new <see cref="GeofenceItem"/> class by name
        /// </summary>
        /// <param name="name">Name of geofence</param>
        public GeofenceItem(string name = default)
        {
            Name = name ?? DefaultName;
            Priority = 0;
            Feature = new Feature();
            BBox = Geometry.DefaultFactory.CreateEmpty(Dimension.False);
        }

        /// <summary>
        /// Instantiates a new <see cref="GeofenceItem"/> class from a GeoJSON feature.
        /// If the feature has a "name" attribute, this geofence's name will be set from that.
        /// </summary>
        public GeofenceItem(IFeature feature)
        {
            Name = feature.Attributes["name"]?.ToString() ?? DefaultName;
            Feature = feature;
            BBox = feature.Geometry.Envelope;

            try
            {
                Priority = Convert.ToInt32(feature.Attributes["priority"]);
            }
            catch
            {
                Priority = 0;
            }
        }

        /// <summary>
        /// Instantiates a new <see cref="GeofenceItem"/> class with name and polygons
        /// </summary>
        /// <param name="name">Name of geofence</param>
        /// <param name="coordinates">Location polygons of geofence</param>
        public GeofenceItem(string name, List<Location> coordinates) : this(name)
        {
            Feature = GeoUtils.LocationsToFeature(coordinates);
            BBox = Feature.Geometry.Envelope;
        }

        #endregion

        #region Static Methods

        public static List<GeofenceItem> FromFile(string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                return FromJsonFile(filePath);
            else
                return FromIniFile(filePath);
        }

        private static List<GeofenceItem> FromJsonFile(string filePath)
        {
            FeatureCollection featureCollection;

            // Read the JSON from the file and deserialize it into a GeoJSON FeatureCollection
            var jsonText = File.ReadAllText(filePath);
            var serializer = GeoJsonSerializer.Create();
            
            using (var stringReader = new StringReader(jsonText))
            using (var jsonReader = new JsonTextReader(stringReader))
                featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);

            if (featureCollection == null)
                throw new JsonException($"Geofence file \"{filePath}\" contained invalid JSON or the JSON did not represent a FeatureCollection");

            // Turn each Feature in the FeatureCollection into a GeofenceItem
            return featureCollection.Select(feature => new GeofenceItem(feature) {
                Filename = Path.GetFileName(filePath)
            }).ToList();
        }

        /// <summary>
        /// Load a geofence from the provided INI file path
        /// </summary>
        /// <param name="filePath">File path of geofence to load</param>
        /// <returns>Returns a geofence object of the file path</returns>
        private static List<GeofenceItem> FromIniFile(string filePath)
        {
            var list = new List<GeofenceItem>();
            // Read all lines of the file and remove all null, empty, or whitespace lines
            var lines = File.ReadAllLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            var locations = new List<Location>();
            string name = "";

            // Loop through each new line of the file
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // If the line starts with '[' then parse the Geofence name
                if (line.StartsWith("[", StringComparison.Ordinal))
                {
                    name = line.TrimStart('[').TrimEnd(']');
                    continue;
                }

                // Trim off any extra whitespace and split the line by a comma ','
                var coordinates = line.Trim('\0').Split(',');
                if (!double.TryParse(coordinates[0], out var lat))
                    continue;

                if (!double.TryParse(coordinates[1], out var lng))
                    continue;

                locations.Add(new Location(lat, lng));

                // If we have reached the end of the file or the start of another
                // geofence, add the current to the list of geofences
                var isEnd = i == lines.Length - 1 || lines[i + 1].StartsWith("[", StringComparison.Ordinal);

                if (isEnd)
                {
                    var geofence = new GeofenceItem(name, locations) {
                        Filename = Path.GetFileName(filePath)
                    };

                    list.Add(geofence);
                    name = "";
                    locations = new List<Location>();
                }
            }

            return list;
        }

        #endregion
    }
}
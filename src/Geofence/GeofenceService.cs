namespace WhMgr.Geofence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using WhMgr.Osm.Models;

    public static class GeofenceService
    {
        const double EquityTolerance = 0.000000001d;

        public static IEnumerable<GeofenceItem> GetGeofences(IEnumerable<GeofenceItem> geofences, Location point)
        {
            // Order descending by priority so that when we iterate forwards using FirstOrDefault, higher-priority
            // geofences are discovered first. Filters initially by bounding box (which is very fast) and then by
            // actual geometry after that.
            var orderedGeofences = geofences.OrderByDescending(g => g.Priority);
            var possibleContaining = orderedGeofences.Where(g => g.BBox.Contains(point));

            return possibleContaining.Where(g => g.Feature.Geometry.Contains(point));
        }

        public static GeofenceItem GetGeofence(IEnumerable<GeofenceItem> geofences, Location point)
            => GetGeofences(geofences, point).FirstOrDefault();

        // Taken from https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
        public static bool IsPointInsidePoly(Location point, List<Location> poly)
        {
            var result = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if ((poly[i].Longitude > point.Longitude) != (poly[j].Longitude > point.Longitude) &&
                    (point.Latitude < (poly[j].Latitude - poly[i].Latitude) * (point.Longitude - poly[i].Longitude) / (poly[j].Longitude - poly[i].Longitude) + poly[i].Latitude))
                {
                    result = !result;
                }
            }
            return result;
        }

        public static Location[] GetIntersectionPoints(Location l1p1, Location l1p2, List<Location> poly)
        {
            var intersectionPoints = new List<Location>();
            for (var i = 0; i < poly.Count; i++)
            {
                var next = (i + 1 == poly.Count) ? 0 : i + 1;
                var ip = GetIntersectionPoint(l1p1, l1p2, poly[i], poly[next]);
                if (ip != null) intersectionPoints.Add(ip);
            }
            return intersectionPoints.ToArray();
        }

        // Math logic from http://www.wyrmtale.com/blog/2013/115/2d-line-intersection-in-c
        public static Location GetIntersectionPoint(Location l1p1, Location l1p2, Location l2p1, Location l2p2)
        {
            var A1 = l1p2.Longitude - l1p1.Longitude;
            var B1 = l1p1.Latitude - l1p2.Latitude;
            var C1 = A1 * l1p1.Latitude + B1 * l1p1.Longitude;

            var A2 = l2p2.Longitude - l2p1.Longitude;
            var B2 = l2p1.Latitude - l2p2.Latitude;
            var C2 = A2 * l2p1.Latitude + B2 * l2p1.Longitude;

            //lines are parallel
            var det = A1 * B2 - A2 * B1;
            if (IsEqual(det, 0d))
                return null; //parallel lines

            var x = (B2 * C1 - B1 * C2) / det;
            var y = (A1 * C2 - A2 * C1) / det;
            var online1 = ((Math.Min(l1p1.Latitude, l1p2.Latitude) < x || IsEqual(Math.Min(l1p1.Latitude, l1p2.Latitude), x))
                && (Math.Max(l1p1.Latitude, l1p2.Latitude) > x || IsEqual(Math.Max(l1p1.Latitude, l1p2.Latitude), x))
                && (Math.Min(l1p1.Longitude, l1p2.Longitude) < y || IsEqual(Math.Min(l1p1.Longitude, l1p2.Longitude), y))
                && (Math.Max(l1p1.Longitude, l1p2.Longitude) > y || IsEqual(Math.Max(l1p1.Longitude, l1p2.Longitude), y))
                );
            var online2 = ((Math.Min(l2p1.Latitude, l2p2.Latitude) < x || IsEqual(Math.Min(l2p1.Latitude, l2p2.Latitude), x))
                && (Math.Max(l2p1.Latitude, l2p2.Latitude) > x || IsEqual(Math.Max(l2p1.Latitude, l2p2.Latitude), x))
                && (Math.Min(l2p1.Longitude, l2p2.Longitude) < y || IsEqual(Math.Min(l2p1.Longitude, l2p2.Longitude), y))
                && (Math.Max(l2p1.Longitude, l2p2.Longitude) > y || IsEqual(Math.Max(l2p1.Longitude, l2p2.Longitude), y))
                );

            if (online1 && online2)
                return new Location(x, y);

            return null; //intersection is at out of at least one segment.
        }

        public static List<Location> GetIntersectionOfPolygons(List<Location> poly1, List<Location> poly2)
        {
            var clippedCorners = new List<Location>();

            // Add the corners of poly1 which are inside poly2       
            for (var i = 0; i < poly1.Count; i++)
            {
                if (IsPointInsidePoly(poly1[i], poly2))
                    AddPoints(clippedCorners, new Location[] { poly1[i] });
            }

            // Add the corners of poly2 which are inside poly1
            for (var i = 0; i < poly2.Count; i++)
            {
                if (IsPointInsidePoly(poly2[i], poly1))
                    AddPoints(clippedCorners, new Location[] { poly2[i] });
            }

            // Add the intersection points
            for (int i = 0, next = 1; i < poly1.Count; i++, next = (i + 1 == poly1.Count) ? 0 : i + 1)
            {
                AddPoints(clippedCorners, GetIntersectionPoints(poly1[i], poly1[next], poly2));
            }

            return OrderClockwise(clippedCorners.ToArray()).ToList();
        }

        public static GeofenceItem PolygonIntersectsWithPolygon(List<GeofenceItem> geofences, MultiPolygon poly)
        {
            var polygon = poly.Select(x => new Location(x[0], x[1])).ToList();
            foreach (var geofence in geofences)
            {
                var coordinates = geofence.Feature.Geometry.Coordinates
                    .Select(x => new Location(x.X, x.Y))
                    .ToList();
                if (GetIntersectionOfPolygons(coordinates, polygon)?.Count > 0)
                {
                    return geofence;
                }    
            }
            return null;
        }

        private static void AddPoints(List<Location> pool, Location[] newpoints)
        {
            foreach (var np in newpoints)
            {
                var found = false;
                foreach (var p in pool)
                {
                    if (IsEqual(p.Latitude, np.Latitude) && IsEqual(p.Longitude, np.Longitude))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) pool.Add(np);
            }
        }

        private static Location[] OrderClockwise(Location[] points)
        {
            double mX = 0;
            double my = 0;
            foreach (var p in points)
            {
                mX += p.Latitude;
                my += p.Longitude;
            }
            mX /= points.Length;
            my /= points.Length;

            return points.OrderBy(v => Math.Atan2(v.Longitude - my, v.Latitude - mX)).ToArray();
        }

        private static bool IsEqual(double d1, double d2)
        {
            return Math.Abs(d1 - d2) <= EquityTolerance;
        }
    }
}
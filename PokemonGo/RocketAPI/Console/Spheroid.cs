using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Console
{
    static class Spheroid
    {
        static double radius = 6376500.0; // earth's mean radius in m

        // Helper function to convert degrees to radians
        public static double DegToRad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        // Helper function to convert radians to degrees
        public static double RadToDeg(double rad)
        {
            return (rad * 180.0 / Math.PI);
        }

        // Calculate the (initial) bearing between two points, in degrees
        public static double CalculateBearing(Location startPoint, Location endPoint)
        {
            double lat1 = DegToRad(startPoint.latitude);
            double lat2 = DegToRad(endPoint.latitude);
            double deltaLon = DegToRad(endPoint.longitude - startPoint.longitude);

            double y = Math.Sin(deltaLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);
            double bearing = Math.Atan2(y, x);

            // since atan2 returns a value between -180 and +180, we need to convert it to 0 - 360 degrees
            return (RadToDeg(bearing) + 360.0) % 360.0;
        }

        // Calculate the destination point from given point having travelled the given distance (in km), on the given initial bearing (bearing may vary before destination is reached)
        public static Location CalculateDestinationLocation(Location point, double bearing, double distance)
        {

            distance = distance / radius; 
            bearing = DegToRad(bearing);

            double lat1 = DegToRad(point.latitude);
            double lon1 = DegToRad(point.longitude);

            double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(distance) + Math.Cos(lat1) * Math.Sin(distance) * Math.Cos(bearing));
            double lon2 = lon1 + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance) * Math.Cos(lat1), Math.Cos(distance) - Math.Sin(lat1) * Math.Sin(lat2));
            lon2 = (lon2 + 3.0 * Math.PI) % (2.0 * Math.PI) - Math.PI; // normalize to -180 - + 180 degrees

            return new Location(RadToDeg(lat2), RadToDeg(lon2));
        }

        // Calculate the distance between two points in m
        public static double CalculateDistanceBetweenLocations(Location startPoint, Location endPoint)
        {
            double latitude = DegToRad(startPoint.latitude); 
            double longitude = DegToRad(startPoint.longitude);
            double num = DegToRad(endPoint.latitude);
            double longitude1 = DegToRad(endPoint.longitude);

            double num1 = longitude1 - longitude;
            double num2 = num - latitude;
            double num3 = Math.Pow(Math.Sin(num2 / 2), 2) + Math.Cos(latitude) * Math.Cos(num) * Math.Pow(Math.Sin(num1 / 2), 2);
            double num4 = 2 * Math.Atan2(Math.Sqrt(num3), Math.Sqrt(1 - num3));
            double num5 = radius * num4;

            return num5;

        }
    }
}

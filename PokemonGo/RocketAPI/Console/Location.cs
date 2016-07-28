namespace PokemonGo.RocketAPI.Console
{
    internal class Location
    {
        public double latitude;
        public double longitude;

        public Location(double v1, double v2)
        {
            this.latitude = v1;
            this.longitude = v2;
        }
    }
}
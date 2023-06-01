namespace WsjtxGps;

public class LocationChangedEventArgs
{
    public LocationChangedEventArgs(Location location)
    {
        Location = location;
    }
    public Location Location { get; set; }
}
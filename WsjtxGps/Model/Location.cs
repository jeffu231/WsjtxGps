namespace WsjtxGps;

public struct Location
{
    public string Grid { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public DateTime TimeStamp { get; init; }

    public override string ToString()
    {
        return $"Lat:{Latitude}, Long:{Longitude}, Grid:{Grid}, Timestamp:{TimeStamp}";
    }
}
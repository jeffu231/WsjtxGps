namespace WsjtxGps.Util;

public static class Coordinates
{
    public static double ConvertToDecimal(int degrees, double minutes, double seconds)
    {
        if (degrees < 0)
        {
            return degrees - (minutes / 60d) - (seconds / 3600d);
        }
        return degrees + (minutes / 60d) + (seconds / 3600d);
    }
}
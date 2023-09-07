using GpsdClient;
using GpsdClient.Models.ConnectionInfo;
using GpsdClient.Models.Events;
using MaidenheadLib;

namespace WsjtxGps.Service;

public class GpsdDevice:IGpsDevice
{
    private readonly ILogger<GpsdDevice> _logger;
    private readonly IConfiguration _config;
    private GpsService? _gpsService;
    private readonly string _defaultGrid;

    public GpsdDevice(ILogger<GpsdDevice> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _defaultGrid = _config.GetValue<string>("GPSD:DefaultGrid")??string.Empty;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GPSD service starting");
        var host = _config.GetValue<string>("GPSD:Host")??string.Empty;
        var poll = _config.GetValue<int>("GPSD:Poll", 30);
        var port = _config.GetValue<int>("GPSD:Port", 2947);
        var info = new GpsdInfo()
        {
            Address = host,
            ReadFrequency = poll * 1000,
            Port = port
        };
        _gpsService = new GpsService(info);
        
        _gpsService.RegisterTpvDataEvent(GpsdServiceOnLocationChanged);
        Connect();
        _logger.LogInformation("GPSD service started");
        return Task.CompletedTask;
    }

    private async Task<bool> Connect()
    {
        //Need to update the gpsd client to be async. Until then this will serve as a workaround. 
        if (_gpsService == null) return false;
        var status = false;
        await Task.Factory.StartNew(() =>
        {
            status = _gpsService.Connect();
        });

        return status;
    }

    private void GpsdServiceOnLocationChanged(object sender, GpsTpvEventArgs e)
    {
        _logger.LogDebug("GPSD service location update {Value}", e.GpsdTpv);
        var latitude = e.GpsdTpv.Latitude;
        var longitude = e.GpsdTpv.Longitude;
        var date = e.GpsdTpv.Time;
        var locator = MaidenheadLocator.LatLngToLocator(latitude, longitude);
        var location = new Location()
        {
            Longitude = longitude,
            Latitude = latitude,
            Grid = locator??_defaultGrid,
            TimeStamp = date
        };
        _logger.LogDebug("GPSD service location update {Value}", location);
        OnLocationUpdated(location);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GPSD service stopping");
        _gpsService?.Disconnect();
        _logger.LogInformation("GPSD service stopped");
        return Task.CompletedTask;
    }
    
    private void OnLocationUpdated(Location location)
    {
        LocationUpdated?.Invoke(this,new LocationChangedEventArgs(location));
    }

    public event EventHandler<LocationChangedEventArgs>? LocationUpdated;
}
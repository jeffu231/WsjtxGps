using System.Collections.Immutable;
using WsjtxClient.Messages.In;
using WsjtxClient.Provider;
using WsjtxGps.Service;

namespace WsjtxGps;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IWsjtxDataProvider _wsjtxDataProvider;
    private readonly IGpsDevice _gpsDevice;

    public Worker(ILogger<Worker> logger, IWsjtxDataProvider wsjtxDataProvider, IGpsDevice gpsDevice)
    {
        _logger = logger;
        _wsjtxDataProvider = wsjtxDataProvider;
        _gpsDevice = gpsDevice;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _gpsDevice.LocationUpdated += GpsDeviceOnLocationUpdated;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken);
        }
    }

    private void GpsDeviceOnLocationUpdated(object? sender, LocationChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Location.Grid) )
        {
            foreach (var instance in _wsjtxDataProvider.Instances)
            {
                var status = _wsjtxDataProvider.Status(instance);
                if (status != null)
                {
                    _logger.LogDebug("Existing grid is {Grid} New grid is {NewGrid}", status.DeGrid, e.Location.Grid);
                
                    if (!status.DeGrid.Equals(e.Location.Grid, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _wsjtxDataProvider.SendMessage(new LocationMessage()
                        {
                            Id = instance,
                            Locator = e.Location.Grid
                        });
                    }
                    else
                    {
                        _logger.LogDebug("Grid already set for instance {Instance}", instance);
                    }
                }
                
            }
        }
        else
        {
            _logger.LogInformation("No grid to set in update");
        }
    }
}
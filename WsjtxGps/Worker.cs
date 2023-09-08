using System.Collections.Immutable;
using WsjtxClient.Messages.In;
using WsjtxClient.Provider;
using WsjtxGps.Service;

namespace WsjtxGps;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly List<IWsjtxDataProvider> _wsjtxDataProviders;
    private readonly IGpsDevice _gpsDevice;
    private readonly IServiceProvider _provider;

    public Worker(ILogger<Worker> logger, IGpsDevice gpsDevice, IServiceProvider provider)
    {
        _logger = logger;
        _gpsDevice = gpsDevice;
        _provider = provider;
        _wsjtxDataProviders = new List<IWsjtxDataProvider>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting GPS update worker");

        var wsjtxDataProviders = _provider.GetServices<IHostedService>()
            .Where(x => x is IWsjtxDataProvider).Cast<IWsjtxDataProvider>();
        
        _logger.LogDebug("Adding {Count} providers", wsjtxDataProviders.Count());
        _wsjtxDataProviders.AddRange(wsjtxDataProviders);
        
        _gpsDevice.LocationUpdated += GpsDeviceOnLocationUpdated;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(60000, stoppingToken);
        }
    }

    private void GpsDeviceOnLocationUpdated(object? sender, LocationChangedEventArgs e)
    {
        _logger.LogDebug("OnLocation Update for {Grid}", e.Location.Grid);
        if (!string.IsNullOrEmpty(e.Location.Grid) )
        {
            foreach (var wsjtxDataProvider in _wsjtxDataProviders)
            {
                _logger.LogDebug("Updating WSJTX Id:{Id}", wsjtxDataProvider.Id);
                foreach (var instance in wsjtxDataProvider.Instances)
                {
                    var status = wsjtxDataProvider.Status(instance);
                    if (status != null)
                    {
                        _logger.LogDebug("Existing grid is {Grid} New grid is {NewGrid}", status.DeGrid, e.Location.Grid);
                
                        if (!status.DeGrid.Equals(e.Location.Grid, StringComparison.CurrentCultureIgnoreCase))
                        {
                            wsjtxDataProvider.SendMessage(new LocationMessage()
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
            
            
        }
        else
        {
            _logger.LogInformation("No grid to set in update");
        }
    }
}
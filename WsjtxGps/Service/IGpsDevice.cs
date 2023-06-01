namespace WsjtxGps.Service;

public interface IGpsDevice: IHostedService
{
    event EventHandler<LocationChangedEventArgs>? LocationUpdated;
}
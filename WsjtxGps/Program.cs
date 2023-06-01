using WsjtxClient.Provider;
using WsjtxGps;
using WsjtxGps.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IGpsDevice, KenwoodRadio>();
        services.AddSingleton<IWsjtxClient, WsjtxClient.Provider.WsjtxClient>();
        services.AddHostedService<IGpsDevice>(provider => provider.GetRequiredService<IGpsDevice>());
        services.AddSingleton<IWsjtxDataProvider, WsjtxDataProvider>();
        services.AddHostedService<IWsjtxDataProvider>(provider => provider.GetRequiredService<IWsjtxDataProvider>());
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
using WsjtxClient.Provider;
using WsjtxGps.Service;

namespace WsjtxGps;

public static class ConfigServiceCollectionExtensions
{
    public static IServiceCollection ConfigureServicesFromConfig(this IServiceCollection services,
        IConfiguration config)
    {
        var gpsType = config.GetValue<string>("GPS:Type")??"gpsd";

        if ("gpsd".Equals(gpsType))
        {
            services.AddSingleton<IGpsDevice, GpsdDevice>();
        }
        else
        {
            services.AddSingleton<IGpsDevice, KenwoodRadio>();
        }
        
        services.AddHostedService<IGpsDevice>(provider => provider.GetRequiredService<IGpsDevice>());

        List<Listener> listeners = new List<Listener>();
        config.GetSection("Wsjtx:Listeners").Bind(listeners);

        services.AddTransient<IWsjtxClient, WsjtxClient.Provider.WsjtxClient>();
        
        
        foreach (var listener in listeners)
        {
            Console.Out.WriteLine($"Adding listener on port {listener.Port}");
            services.AddSingleton<IHostedService>(x => new WsjtxDataProvider(
                x.GetRequiredService<ILogger<WsjtxDataProvider>>(),
                x.GetRequiredService<IWsjtxClient>(),
                listener));
        }
        
        

        return services;
    }
}
using WsjtxGps;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.ConfigureServicesFromConfig(builder.Configuration);
IHost host = builder.Build();

host.Run();
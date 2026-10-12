using ApexClock.Service;
using Microsoft.Extensions.Hosting.WindowsServices;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService((WindowsServiceLifetimeOptions options) => options.ServiceName = "ApexClock");
builder.Services.AddSingleton<ScheduleStore>();
builder.Services.AddSingleton<ClientHub>();
builder.Services.AddHostedService<RotationWorker>();
builder.Services.AddHostedService<PipeServer>();

builder.Build().Run();

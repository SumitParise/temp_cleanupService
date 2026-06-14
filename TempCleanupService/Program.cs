using TempCleanupService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Temp Cleanup Service";
});

builder.Services.Configure<CleanupOptions>(
    builder.Configuration.GetSection(CleanupOptions.SectionName));
builder.Services.AddSingleton<TempCleanupRunner>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

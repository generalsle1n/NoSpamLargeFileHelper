using NospamHelper;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddSingleton<NoSpamHelper>();
        services.AddSingleton<VirustotalHelper>();
    })
    .Build();

await host.RunAsync();

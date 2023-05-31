using NospamHelper;
using Quartz;
using Serilog;
using System.Reflection;

JobKey MainJob = new JobKey("MainJob");

string binaryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string logPathFolder = Path.Combine(binaryPath, "Logs","VirusTotal.log");
string configPath = Path.Combine(binaryPath, "appsettings.json");

IConfiguration tempConfig = new ConfigurationBuilder()
    .AddJsonFile(configPath)
    .Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddSingleton<NoSpamHelper>();
        services.AddSingleton<VirustotalHelper>();
    })
    .Build();

await host.RunAsync();

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
        services.AddSingleton<NoSpamHelper>();
        services.AddSingleton<VirustotalHelper>();
        services.AddSingleton<MailHelper>();

        services.AddQuartzHostedService(settings =>
        {
            settings.WaitForJobsToComplete = true;
        });

        services.AddQuartz(queue =>
        {
            queue.UseMicrosoftDependencyInjectionJobFactory();
            queue.AddJob<Worker>(job =>
            {
                job.WithIdentity(MainJob);
            });

            queue.AddTrigger(trigger =>
            {
                trigger.StartNow();
                trigger.WithSimpleSchedule(schedule =>
                {
                    schedule.WithIntervalInMinutes(tempConfig.GetValue<int>("IntervalInMinutes"));
                    schedule.RepeatForever();
                });
                trigger.ForJob(MainJob);
            });
        });
    })
    .ConfigureLogging(log =>
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(logPathFolder, rollingInterval: RollingInterval.Month)
            .CreateLogger();
    })
    .UseWindowsService(conf =>
    {
        conf.ServiceName = "NoSpamHelper";
    })
    .UseSerilog()
    .Build();

await host.RunAsync();
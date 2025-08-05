using FolderSynchronizer.Interfaces;
using FolderSynchronizer.Options;
using FolderSynchronizer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        if (args.Length > 0)
        {
            // If command line arguments are provided, use them to configure WorkerOptions
            // Note: there is no validation here, so ensure the arguments are in the correct format and order.
            services.Configure<WorkerOptions>(options =>
            {
                options.SyncInterval = int.Parse(args[0]);
                options.LogFileFullPath = args.Length > 1 ? args[1] : string.Empty;
                options.CopyFromPath = args.Length > 2 ? args[2] : string.Empty;
                options.CopyToPath = args.Length > 3 ? args[3] : string.Empty;
            });
        }
        // Register WorkerOptions from appsettings.json if no command line arguments are provided to provide the option of default values and easy configuration.
        else
        {
            services.Configure<WorkerOptions>(context.Configuration.GetSection("WorkerOptions"));
        }

        services.AddSingleton<IFileSynchronizationService, FileSynchronizationService>();
        services.AddScoped<ILogWriter, LogWriter>();

        // WorkerService is registered as a hosted service, which will run in the background
        services.AddHostedService<WorkerService>();
    });

var app = builder.Build();

app.Run();
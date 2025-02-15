using Hangfire;
using Hangfire.SqlServer;
using FlexScheduler.Services;
using FlexScheduler.Models;
using Microsoft.Extensions.Options;
using FlexScheduler.Jobs;

namespace FlexScheduler.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Hangfire settings
        services.Configure<HangfireSettings>(configuration.GetSection("HangfireSettings"));

        services.AddHangfire((sp, config) =>
        {
            var connectionString = configuration.GetConnectionString("HangfireConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Hangfire connection string is not configured");
            }

            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                });
        });

        // Configure Hangfire servers based on settings
        var settings = configuration.GetSection("HangfireSettings").Get<HangfireSettings>();
        if (settings?.ServerList != null && settings.ServerList.Any())
        {
            foreach (var server in settings.ServerList)
            {
                services.AddHangfireServer(options =>
                {
                    options.ServerName = server.Name;
                    options.WorkerCount = server.WorkerCount;
                    options.Queues = server.QueueList.ToArray();
                });
            }
        }
        else
        {
            // Fallback default configuration
            services.AddHangfireServer(options =>
            {
                options.ServerName = "default-server";
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = new[] { "default", "critical" };
            });
        }

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LoginSettings>(configuration.GetSection("LoginSettings"));

        services.AddHttpClient("LoginService", (sp, client) =>
        {
            var loginSettings = sp.GetRequiredService<IOptions<LoginSettings>>().Value;
            client.BaseAddress = new Uri(loginSettings.LoginEndpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IHttpJob, HttpJob>();
        services.AddScoped<IJobService, JobService>();
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}
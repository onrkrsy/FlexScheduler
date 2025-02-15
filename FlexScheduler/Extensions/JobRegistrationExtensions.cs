using Hangfire;
using FlexScheduler.Jobs;
using FlexScheduler.Models;
using FlexScheduler.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;
using Hangfire.Storage;

namespace FlexScheduler.Extensions;

public static class JobRegistrationExtensions
{
    public static void AddRecurringJobs(this IRecurringJobManager recurringJobManager, IServiceProvider serviceProvider)
    {
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IJobService>>();
        var jobManager = scope.ServiceProvider.GetRequiredService<IJobService>();

        RegisterCustomJobs(recurringJobManager, scope.ServiceProvider, true);
        LoadHttpJobsFromConfiguration(jobManager, logger);
    }

    private static void RegisterCustomJobs(IRecurringJobManager recurringJobManager, IServiceProvider serviceProvider, bool clearAllJobs)
    {
        if (clearAllJobs)
        {
            var allJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            foreach (var job in allJobs)
            {
                RecurringJob.RemoveIfExists(job.Id);
            }
        }

        var jobs = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseRecurringJob)) && !t.IsAbstract);

        foreach (var job in jobs)
        {
            var jobInstance = (BaseRecurringJob)ActivatorUtilities.CreateInstance(serviceProvider, job);
            RecurringJob.AddOrUpdate(
                jobInstance.JobId,
                jobInstance.Queue,
                () => jobInstance.Execute(CancellationToken.None),
                jobInstance.CronExpression
            );
        }
    }

    private static void LoadHttpJobsFromConfiguration(IJobService jobManager, ILogger logger)
    {
        try
        {
            var jobsFilePath = Path.Combine("Configurations", "httpJobs.json");
            if (!File.Exists(jobsFilePath))
            {
                logger.LogWarning("Jobs configuration file not found at {Path}", jobsFilePath);
                return;
            }

            var jobsJson = File.ReadAllText(jobsFilePath);
            var jobsConfig = JsonConvert.DeserializeObject<JobConfiguration>(jobsJson);

            if (jobsConfig?.Jobs == null || !jobsConfig.Jobs.Any())
            {
                logger.LogWarning("No jobs found in configuration");
                return;
            }

            foreach (var job in jobsConfig.Jobs.Where(j => j.IsEnabled))
            {
                try
                {
                    logger.LogInformation("Configuring job: {JobId}", job.JobId);

                    var request = new RecurringHttpJobRequest
                    {
                        JobId = job.JobId,
                        CronExpression = job.CronExpression,
                        Url = job.Url,
                        HttpMethod = job.HttpMethod,
                        RequiresAuthentication = job.RequiresAuthentication,
                        TimeoutInSeconds = job.TimeoutInSeconds,
                        Headers = job.Headers ?? new Dictionary<string, string>(),
                        Payload = job.Payload
                    };

                    jobManager.CreateRecurringHttpJob(request);
                    logger.LogInformation("Job configured successfully: {JobId} [{Tags}]", 
                        job.JobId, string.Join(", ", job.Tags));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error configuring job {JobId}", job.JobId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading jobs from configuration");
        }
    }
} 
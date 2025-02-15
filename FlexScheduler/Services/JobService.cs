using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.Json;
using FlexScheduler.Models;

namespace FlexScheduler.Services;

public class JobService : IJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobService> _logger;

    public JobService(IServiceProvider serviceProvider, ILogger<JobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void CreateRecurringHttpJob(RecurringHttpJobRequest request)
    {
        _logger.LogInformation("Received request: {Request}", JsonConvert.SerializeObject(request));

        string payloadJson;
        if (request.Payload is JsonElement jsonElement)
        {
            payloadJson = jsonElement.GetRawText();
        }
        else
        {
            payloadJson = JsonConvert.SerializeObject(request.Payload);
        }

        var job = _serviceProvider.GetRequiredService<IHttpJob>();
        RecurringJob.AddOrUpdate(
            request.JobId,
            request.Queue,
            () => job.Execute(
                request.Url,
                request.HttpMethod,
                payloadJson,
                request.JobId,
                request.Headers,
                request.TimeoutInSeconds,
                request.RequiresAuthentication,
                CancellationToken.None),
            request.CronExpression
        );
        _logger.LogInformation("Created recurring HTTP job: {JobId} (Auth Required: {RequiresAuth})", 
            request.JobId, request.RequiresAuthentication);
    }

    public string CreateDelayedHttpJob(DelayedHttpJobRequest request)
    {
        var job = _serviceProvider.GetRequiredService<IHttpJob>();
        string payloadJson;
        if (request.Payload is JsonElement jsonElement)
        {
            payloadJson = jsonElement.GetRawText();
        }
        else
        {
            payloadJson = JsonConvert.SerializeObject(request.Payload);
        }

        var jobId = BackgroundJob.Schedule(
            () => job.Execute(
                request.Url,
                request.HttpMethod,
                payloadJson,
                request.JobId,
                request.Headers,
                request.TimeoutInSeconds,
                request.RequiresAuthentication,
                CancellationToken.None),
            request.Delay
        );

        _logger.LogInformation("Created delayed HTTP job with Hangfire ID: {JobId} (Auth Required: {RequiresAuth})", 
            jobId, request.RequiresAuthentication);
        return jobId;
    }

    public void DeleteJob(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
        _logger.LogInformation("Deleted recurring job: {JobId}", jobId);
    }

    public bool JobExists(string jobId)
    {
        try
        {
            var job = JobStorage.Current.GetConnection().GetRecurringJobs()
                .FirstOrDefault(x => x.Id == jobId);
            return job != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking job existence: {JobId}", jobId);
            throw;
        }
    }
} 
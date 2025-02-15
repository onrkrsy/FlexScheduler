using FlexScheduler.Models;

namespace FlexScheduler.Services;

public interface IJobService
{
    void CreateRecurringHttpJob(RecurringHttpJobRequest request);
    string CreateDelayedHttpJob(DelayedHttpJobRequest request);
    void DeleteJob(string jobId);
    bool JobExists(string jobId);
} 
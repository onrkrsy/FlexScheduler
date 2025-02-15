using Microsoft.AspNetCore.Mvc;
using FlexScheduler.Models;
using FlexScheduler.Services;

namespace FlexScheduler.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    [HttpPost("recurring")]
    public IActionResult CreateRecurringJob([FromBody] RecurringHttpJobRequest request)
    {
        try
        {
            _jobService.CreateRecurringHttpJob(request);
            return Ok(new { JobId = request.JobId, Status = "Created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recurring job");
            return StatusCode(500, new { Error = "Failed to create recurring job", Message = ex.Message });
        }
    }

    [HttpPost("delayed")]
    public IActionResult CreateDelayedJob([FromBody] DelayedHttpJobRequest request)
    {
        try
        {
            var jobId = _jobService.CreateDelayedHttpJob(request);
            return Ok(new { JobId = jobId, Status = "Created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delayed job");
            return StatusCode(500, new { Error = "Failed to create delayed job", Message = ex.Message });
        }
    }

    [HttpDelete("{jobId}")]
    public IActionResult DeleteJob(string jobId)
    {
        try
        {
            if (!_jobService.JobExists(jobId))
            {
                return NotFound(new { Error = "Job not found", JobId = jobId });
            }

            _jobService.DeleteJob(jobId);
            return Ok(new { JobId = jobId, Status = "Deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job");
            return StatusCode(500, new { Error = "Failed to delete job", Message = ex.Message });
        }
    }

    [HttpGet("{jobId}/exists")]
    public IActionResult JobExists(string jobId)
    {
        try
        {
            var exists = _jobService.JobExists(jobId);
            return Ok(new { JobId = jobId, Exists = exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking job existence");
            return StatusCode(500, new { Error = "Failed to check job existence", Message = ex.Message });
        }
    }
} 
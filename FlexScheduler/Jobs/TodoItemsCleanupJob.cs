using Microsoft.Extensions.Logging;

namespace FlexScheduler.Jobs;

public class TodoItemsCleanupJob : BaseRecurringJob
{
    private readonly HttpClient _httpClient;

    public TodoItemsCleanupJob(
        ILogger<TodoItemsCleanupJob> logger,
        IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public override string JobId => "TodoItems-Cleanup-Job";
    public override string CronExpression => "0 0 * * *"; // At midnight every day
    public override string Queue => "maintenance";

    public override async Task Execute(CancellationToken cancellationToken)
    {
        await ExecuteWithLogging(async (token) =>
        {  
            try
            {
                // Cleanup parameters
                var olderThanDays = 30;
                var status = "completed";
                var baseUrl = "http://localhost:5001";

                // Get items to cleanup
                var itemsToDelete = await GetItemsToCleanup(olderThanDays, status, token);
                _logger.LogInformation("Found {Count} items to cleanup", itemsToDelete.Count);

                // Process items in batches
                var batchSize = 100;
                for (var i = 0; i < itemsToDelete.Count; i += batchSize)
                {
                    var batch = itemsToDelete.Skip(i).Take(batchSize).ToList();
                    await ProcessBatch(batch, token);
                    _logger.LogInformation("Processed batch {BatchNumber} of {TotalBatches}",
                        (i / batchSize) + 1, Math.Ceiling(itemsToDelete.Count / (double)batchSize));
                }

                // Archive deleted items
                await ArchiveItems(itemsToDelete.Select(x => x.Id).ToList(), token);
                _logger.LogInformation("Archived {Count} items", itemsToDelete.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TodoItems cleanup");
                throw;
            }
        }, cancellationToken);
    }

    private async Task<List<TodoItem>> GetItemsToCleanup(int olderThanDays, string status, CancellationToken cancellationToken)
    {
        var url = $"http://localhost:5001/api/TodoItems/search?olderThanDays={olderThanDays}&status={status}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<TodoItem>>(cancellationToken: cancellationToken);
        return items ?? new List<TodoItem>();
    }

    private async Task ProcessBatch(List<TodoItem> batch, CancellationToken cancellationToken)
    {
        foreach (var item in batch)
        {
            try
            {
                // Soft delete the item
                var deleteUrl = $"http://localhost:5001/api/TodoItems/{item.Id}";
                var response = await _httpClient.DeleteAsync(deleteUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully deleted item {ItemId}", item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId}", item.Id);
                // Continue with other items even if one fails
            }

            // Add small delay to prevent overwhelming the server
            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task ArchiveItems(List<int> itemIds, CancellationToken cancellationToken)
    {
        var archiveUrl = "http://localhost:5001/api/TodoItems/archive";
        var content = JsonContent.Create(new { ItemIds = itemIds });
        var response = await _httpClient.PostAsync(archiveUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
namespace FlexScheduler.Models;

public class HangfireSettings
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public List<HangfireServer> ServerList { get; set; } = new();
}

public class HangfireServer
{
    public string Name { get; set; } = null!;
    public int WorkerCount { get; set; }
    public List<string> QueueList { get; set; } = new();
} 
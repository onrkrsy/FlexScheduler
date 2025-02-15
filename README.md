# FlexScheduler

FlexScheduler is a flexible HTTP job scheduling service built with .NET 8 and Hangfire. It provides a simple way to schedule and manage HTTP jobs with support for both recurring and delayed execution.

## Features

- Schedule recurring HTTP jobs using cron expressions
- Schedule delayed HTTP jobs with custom delay intervals
- Support for job authentication via bearer tokens
- Configurable HTTP timeouts and retry policies
- JSON configuration for predefined jobs
- Multiple server support with queue prioritization
- Secure Hangfire dashboard with basic authentication
- RESTful API for job management

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

## Quick Start

1. Clone the repository
2. Update the connection strings and settings in `appsettings.json`
3. Run the following commands:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```
4. Access the Hangfire dashboard at `/hangfire` (default credentials: admin/admin)

## Configuration

### Connection String

Update the `appsettings.json` file with your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Server=(localdb)\\mssqllocaldb;Database=FlexScheduler;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Hangfire Settings

Configure the Hangfire dashboard authentication and server settings:

```json
{
  "HangfireSettings": {
    "UserName": "admin",
    "Password": "your-secure-password",
    "ServerList": [
      {
        "Name": "default-server",
        "WorkerCount": 5,
        "QueueList": [
          "critical",
          "default",
          "long-running"
        ]
      },
      {
        "Name": "background-server",
        "WorkerCount": 2,
        "QueueList": [
          "background",
          "low-priority"
        ]
      }
    ]
  }
}
```

### HTTP Jobs Configuration

Define your recurring HTTP jobs in `Configurations/httpJobs.json`. Here are some examples:

```json
{
  "Jobs": [
    {
      "JobId": "WeatherForecast-HealthCheck",
      "CronExpression": "*/5 * * * *",
      "Url": "http://localhost:5000/WeatherForecast",
      "HttpMethod": "GET",
      "RequiresAuthentication": false,
      "TimeoutInSeconds": 30,
      "IsEnabled": true,
      "Tags": [ "weather-service", "monitoring" ],
      "Description": "Weather Forecast API health check - every 5 minutes"
    },
    {
      "JobId": "TodoItems-Cleanup",
      "CronExpression": "0 0 * * *",
      "Url": "http://localhost:5001/api/TodoItems/cleanup",
      "HttpMethod": "POST",
      "RequiresAuthentication": true,
      "TimeoutInSeconds": 300,
      "Payload": {
        "olderThanDays": 30,
        "status": "completed"
      },
      "IsEnabled": true,
      "Tags": [ "todo-service", "maintenance" ],
      "Description": "Clean up completed todo items older than 30 days - runs daily at midnight"
    }
  ]
}
```

### Authentication Settings

If your jobs require authentication, configure the login settings:

```json
{
  "LoginSettings": {
    "UserName": "your-username",
    "Password": "your-password",
    "Application": "FlexScheduler",
    "SystemUserType": "System",
    "LoginEndpoint": "http://your-auth-server/api/auth/login"
  }
}
```

## API Endpoints

### Create Recurring Job
```http
POST /api/jobs/recurring
Content-Type: application/json

{
  "jobId": "my-job",
  "cronExpression": "*/15 * * * *",
  "url": "http://api.example.com/endpoint",
  "httpMethod": "POST",
  "requiresAuthentication": true,
  "timeoutInSeconds": 60,
  "queue": "default",
  "payload": { "key": "value" }
}
```

### Create Delayed Job
```http
POST /api/jobs/delayed
Content-Type: application/json

{
  "jobId": "my-delayed-job",
  "url": "http://api.example.com/endpoint",
  "httpMethod": "POST",
  "delay": "00:05:00",
  "requiresAuthentication": true,
  "timeoutInSeconds": 60,
  "queue": "background",
  "payload": { "key": "value" }
}
```

### Delete Job
```http
DELETE /api/jobs/{jobId}
```

### Check Job Existence
```http
GET /api/jobs/{jobId}/exists
```

## Queue Prioritization

Jobs can be assigned to different queues based on their priority:
- `critical`: High-priority jobs that need immediate processing
- `default`: Standard jobs with normal priority
- `long-running`: Jobs that take longer to complete
- `background`: Low-priority background tasks
- `low-priority`: Lowest priority tasks that can wait

Configure server workers to process specific queues in `HangfireSettings.ServerList`.

## Security Considerations

1. **Dashboard Security**:
   - Change the default dashboard credentials in production
   - Use a strong password for the dashboard
   - Consider implementing IP restrictions

2. **Job Authentication**:
   - Store sensitive credentials in secure configuration storage
   - Use environment-specific settings files
   - Consider using Azure Key Vault or similar services

3. **Network Security**:
   - Use HTTPS for all endpoints
   - Implement proper network segmentation
   - Configure appropriate timeouts

## Monitoring

1. Access the Hangfire dashboard at `/hangfire`
2. Monitor job execution status and history
3. View real-time statistics and server health
4. Check failed jobs and retry them if needed

## Troubleshooting

1. **Job Failures**:
   - Check the job details in the Hangfire dashboard
   - Review application logs for error messages
   - Verify endpoint availability and authentication

2. **Dashboard Access Issues**:
   - Verify credentials in `HangfireSettings`
   - Check network connectivity
   - Review server logs for authentication errors

3. **Performance Issues**:
   - Monitor worker count and queue lengths
   - Adjust server configuration if needed
   - Consider adding more workers for busy queues

## License

This project is licensed under the MIT License.

# FlexScheduler

## Giriş

FlexScheduler, zamanlanmış görevleri yönetmek için kullanılan bir uygulamadır.

## Kurulum

Projeyi klonladıktan sonra, bağımlılıkları yüklemek için aşağıdaki komutu çalıştırın:

```
dotnet restore
```

## Kullanım

Uygulamayı çalıştırmak için aşağıdaki komutu kullanın:

```
dotnet run
```

## IsAuthenticated Ayarı

`IsAuthenticated` ayarı, login servisi bilgileri için gereklidir. Eğer bir mikroserviste `IsAuthenticated` ayarı varsa, auth veya login servis bilgilerini doldurmanız gerekmektedir. Bu ayar, `client-id` ve `secret` bilgileriyle login servisine gidip token alır ve bu tokenı isteklerine ekler.

## Katkıda Bulunma

Katkıda bulunmak için lütfen bir pull request gönderin.

## Lisans

Bu proje MIT Lisansı ile lisanslanmıştır.
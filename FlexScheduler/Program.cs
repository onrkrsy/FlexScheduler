using FlexScheduler.Extensions;
using FlexScheduler.Models;
using Hangfire;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add custom services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
 
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Configure Hangfire dashboard with authentication
var hangfireSettings = app.Services.GetRequiredService<IOptions<HangfireSettings>>();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "FlexScheduler Jobs",
    Authorization = new[] { new HangfireAuthorizationFilter(hangfireSettings) }
});

app.MapControllers();

// Register recurring jobs
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddRecurringJobs(app.Services);

app.Run();
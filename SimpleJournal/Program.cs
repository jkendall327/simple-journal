using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var config = builder.Configuration.GetSection("MoodTracker");

builder.Services.AddSingleton<MoodTracker>();
builder.Services.Configure<MoodTrackerOptions>(config);

using var host = builder.Build();
var moodTracker = host.Services.GetRequiredService<MoodTracker>();
await moodTracker.ProcessTodayEntry();

await host.RunAsync();
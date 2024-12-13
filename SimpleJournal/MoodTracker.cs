using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleJournal;

public class MoodTracker
{
    private readonly ILogger<MoodTracker> _logger;
    private readonly MoodTrackerOptions _options;
    private readonly string _entriesPath;
    private readonly string _masterFilePath;

    private const string Template = """
                                    Date: {0}

                                    Current Mood (1-10): 

                                    What's on your mind today?

                                    Current Annoyances:

                                    Highlights of the day:

                                    Energy Level (1-10):

                                    Sleep Quality (1-10):

                                    Additional Notes:

                                    Goals for tomorrow:

                                    """;

    public MoodTracker(ILogger<MoodTracker> logger, IOptions<MoodTrackerOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        _entriesPath = Path.Combine(_options.BaseDirectory, _options.DailyEntriesSubDir);
        _masterFilePath = Path.Combine(_options.BaseDirectory, _options.MasterFileName);
    }

    public async Task ProcessTodayEntry()
    {
        try
        {
            InitializeDirectories();
            
            var todayFile = await CreateTodayEntry();
            
            await WaitForEditorToClose(todayFile);
            
            await ConsolidateEntries();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing today's entry");
            throw;
        }
    }

    private void InitializeDirectories()
    {
        var directory = _options.BaseDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            var assembly = Assembly.GetExecutingAssembly().Location;
            directory = Path.GetDirectoryName(assembly);
        }
        
        Directory.CreateDirectory(directory!);
        Directory.CreateDirectory(_entriesPath);
        
        _logger.LogDebug("Initialized directories at {BaseDir}", directory);
    }

    private async Task<string> CreateTodayEntry()
    {
        var todayFileName = $"mood_{DateTime.Now:yyyy-MM-dd}.txt";
        var todayFilePath = Path.Combine(_entriesPath, todayFileName);

        if (File.Exists(todayFilePath)) return todayFilePath;
        
        var template = string.Format(Template, DateTime.Now.ToString("yyyy-MM-dd"));
        await File.WriteAllTextAsync(todayFilePath, template);
        _logger.LogInformation("Created new entry for {Date}", DateTime.Now.ToString("yyyy-MM-dd"));

        return todayFilePath;
    }

    private async Task WaitForEditorToClose(string filePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            };
            
            using var process = Process.Start(processInfo);
            
            if (process == null)
            {
                _logger.LogWarning("Could not get process handle for editor");
                return;
            }

            _logger.LogInformation("Waiting for editor to close...");
            await process.WaitForExitAsync();
            _logger.LogInformation("Editor closed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for editor process");
            throw;
        }
    }

    private async Task ConsolidateEntries()
    {
        try
        {
            var entries = Directory
                .GetFiles(_entriesPath, "mood_*.txt")
                .OrderBy(f => f)
                .ToList();

            var consolidated = new StringBuilder();
            consolidated.AppendLine("=== Mood History ===\n");

            foreach (var entryPath in entries)
            {
                var content = await File.ReadAllTextAsync(entryPath);
                consolidated.AppendLine($"=== {Path.GetFileNameWithoutExtension(entryPath)} ===");
                consolidated.AppendLine(content);
                consolidated.AppendLine("\n-------------------\n");
            }

            await File.WriteAllTextAsync(_masterFilePath, consolidated.ToString());
            _logger.LogInformation("Successfully consolidated {Count} entries", entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating entries");
            throw;
        }
    }
}
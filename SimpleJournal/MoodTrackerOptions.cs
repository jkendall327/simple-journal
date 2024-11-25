public class MoodTrackerOptions
{
    public string BaseDirectory { get; set; } = string.Empty;
    public string DailyEntriesSubDir { get; set; } = "daily_entries";
    public string MasterFileName { get; set; } = "mood_history.txt";
    public bool WaitForEditor { get; set; } = true;
}
namespace FiszkiApp.Models;

using System.Diagnostics;

public class Statistics
{
    public Statistics(string user, string subject, string batch, string time, string efficiency, string date)
    {
        this.user = user;
        this.subject = subject;
        this.batch = batch;
        this.time = time;
        this.efficiency = efficiency;
        this.date = date;
    }

    public string user { get; set; }
    public string subject { get; set; }
    public string batch { get; set; }
    public string time { get; set; }
    public string date { get; set; }
    public string efficiency { get; set; }
    public static float skips { get; set; }
    public static float batchSize { get; set; }
    public static Stopwatch stopwatch = new Stopwatch();
    public static bool stopwatchStarted = false;
}
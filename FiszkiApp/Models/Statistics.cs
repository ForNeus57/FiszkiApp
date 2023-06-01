namespace FiszkiApp.Models;

using System.Diagnostics;

public class Statistics
{
    public static float skips { get; set; }
    public static float batchSize { get; set; }
    public static Stopwatch stopwatch = new Stopwatch();
    public static bool stopwatchStarted = false;
}
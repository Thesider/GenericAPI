namespace GenericAPI.Models;

public class MetricEntry
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = "Gauge"; // Counter, Gauge, Histogram, Summary
}

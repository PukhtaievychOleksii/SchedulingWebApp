namespace SchedulingWebApp.Models;

public class ScheduleParameter
{
    public string Name { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; }

    public ScheduleParameter(string name, int minValue, int maxValue)
    {
        Name = name;
        MinValue = minValue;
        MaxValue = maxValue;
    }
}
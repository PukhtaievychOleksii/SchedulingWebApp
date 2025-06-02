namespace SchedulingWebApp.Models;

public class DataItem
{
    public string ScheduleName { get; set; }
    public int Value { get; set; }

    public DataItem(string schedule, int value)
    {
        ScheduleName = schedule;
        Value = value;
    }
}

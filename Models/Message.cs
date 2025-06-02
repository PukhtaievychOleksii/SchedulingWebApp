namespace SchedulingWebApp.Models;

public enum MessageRole
{
    User,
    Assistant
}
public class Message
{
    public MessageRole Role { get; set; } // "user" or "assistant"
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}


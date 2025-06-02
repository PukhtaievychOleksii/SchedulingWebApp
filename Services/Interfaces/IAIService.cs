using SchedulingWebApp.Models;

namespace SchedulingWebApp.Services.Interfaces;

public interface IAIService
{
    Task<string> SendRequestAsync(string prompt, Schedule schedule);
    string SaveScheduleContext(Schedule schedule);
}

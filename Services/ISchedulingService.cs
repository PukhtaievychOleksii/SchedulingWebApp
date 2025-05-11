using SchedulingWebApp.Models;

namespace SchedulingWebApp.Services;

public interface ISchedulingService
{
    public SchedulingResult Generate(int[,,] shiftPreferences, bool[,,] unavailabilityRequests);
}
using SchedulingWebApp.Models;

namespace SchedulingWebApp.Services.Interfaces;

public interface ISchedulingService
{
    public SchedulingResult Generate(int[,,] shiftPreferences, bool[,,] unavailabilityRequests);
}
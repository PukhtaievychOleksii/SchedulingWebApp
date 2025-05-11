using SchedulingWebApp.Services;

namespace SchedulingWebApp.Models;

public struct Schedule
{
    public SchedulingResult SchedulingResult;
    public bool[,,] ShiftPreferences;
    public bool[,,] UnavailabilityRequests;
    public int[] UnsatisfiedPreferencesCount;
    public int[] PreferencesCount;
    public int[] UnsatisfactionStreak;
    public int[] UnavailabilityCount;
    public Schedule()
    {
        SchedulingResult = new SchedulingResult();
        ShiftPreferences = new bool[SchedulingService.NumEmployees, SchedulingService.NumDays, SchedulingService.NumShifts];
        UnavailabilityRequests = new bool[SchedulingService.NumEmployees, SchedulingService.NumDays, SchedulingService.NumShifts];
        UnsatisfiedPreferencesCount = new int[SchedulingService.NumEmployees];
        PreferencesCount = new int[SchedulingService.NumEmployees];
        UnsatisfactionStreak = new int[SchedulingService.NumEmployees];
        UnavailabilityCount = new int[SchedulingService.NumEmployees];
    }
}

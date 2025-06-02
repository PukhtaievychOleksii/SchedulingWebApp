using SchedulingWebApp.Services;

namespace SchedulingWebApp.Models;

public struct Schedule
{
    public SchedulingResult SchedulingResult;
    public bool[,,] ShiftPreferences;
    public bool[,,] UnavailabilityRequests;
    public int[] UnsatisfiedPreferencesCount;
    public int[] TotalPreferencesCount;
    public int[] SatisfiedPreferencesCount;
    public int[] UnsatisfactionStreak;
    public int[] UnavailabilityCount;
    public int[] UnsatisfactionPercentage;
    public int[] RolePercentage;
    public int[] NextPriority;
    public string Name;
    public Dictionary<ScheduleParameter, int[]> ScheduleParametersSnapshot;
    public static Dictionary<ScheduleParameter, int[]> GlobalScheduleParameters = new Dictionary<ScheduleParameter, int[]>();



    public Schedule()
    {
        SchedulingResult = new SchedulingResult();
        ShiftPreferences = new bool[SchedulingService.NumEmployees, SchedulingService.NumDays, SchedulingService.NumShifts];
        UnavailabilityRequests = new bool[SchedulingService.NumEmployees, SchedulingService.NumDays, SchedulingService.NumShifts];
        UnsatisfiedPreferencesCount = new int[SchedulingService.NumEmployees];
        TotalPreferencesCount = new int[SchedulingService.NumEmployees];
        UnsatisfactionStreak = new int[SchedulingService.NumEmployees];
        UnavailabilityCount = new int[SchedulingService.NumEmployees];
        UnsatisfactionPercentage = new int[SchedulingService.NumEmployees];
        RolePercentage = new int[SchedulingService.NumEmployees];
        for(int e = 0; e < SchedulingService.NumEmployees; e++)
        {
            var employee = SchedulingService.AllEmployees[e];
            RolePercentage[e] = GetRolePercentage(employee.Role);
        }
        Name = "";
        NextPriority = new int[SchedulingService.NumEmployees];
        ScheduleParametersSnapshot = new Dictionary<ScheduleParameter, int[]>();
    }
    private int GetRolePercentage(Role role)
    {
        int index = Array.IndexOf(Enum.GetValues(typeof(Role)), role);
        int rolesNumber = Enum.GetNames(typeof(Role)).Length;
        return (rolesNumber - index) * (100 / rolesNumber);

    }

    public void AddScheduleParameter(ScheduleParameter parameter, int[] values)
    {
        ScheduleParametersSnapshot.Add(parameter, values);
    }

    public void SetScheduleParameterValues(ScheduleParameter parameter, int[] values)
    {
        if (ScheduleParametersSnapshot.ContainsKey(parameter))
        {
            ScheduleParametersSnapshot[parameter] = values;
        }
    }

    public int[] GetScheduleParameterValues(ScheduleParameter parameter)
    {
        if (ScheduleParametersSnapshot.ContainsKey(parameter))
        {
            return ScheduleParametersSnapshot[parameter];
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public void MakeParametersSnapshot()
    {
        ScheduleParametersSnapshot = GlobalScheduleParameters.ToDictionary(
            entry => entry.Key,
            entry => (int[])entry.Value.Clone()
        );
    }

    public static void ResetParameters()
    {
        GlobalScheduleParameters = new Dictionary<ScheduleParameter, int[]>();
    }
}

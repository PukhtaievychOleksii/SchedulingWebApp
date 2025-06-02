using Google.OrTools.Sat;
using Radzen.Blazor;
using SchedulingWebApp.Models;
using SchedulingWebApp.Services.Interfaces;
using System;
using System.Runtime.CompilerServices;

namespace SchedulingWebApp.Services;

public class TestingService: ITestingService
{
    private CancellationTokenSource? _cts;
    private Task? _testingTask;
    private readonly object _lock = new();
    private Schedule _newSchedule = new Schedule();
    private List<Schedule> _schedules;
    private int _numberOfSchedulesPerTest = 5;
    private int _totalSchedules = 0;
    private int _totalTests = 0;
    private int _maxDissatisfactionStreak = 0;
    private bool _isTesting = false;

    public void Start(ISchedulingService schedulingService, int schedulesPerTest)
    {
        lock (_lock)
        {
            if (_isTesting) return;

            _cts = new CancellationTokenSource();
            _isTesting = true;
            _totalSchedules = 0;
            _totalTests = 0;
            _maxDissatisfactionStreak = 0;
            _numberOfSchedulesPerTest = schedulesPerTest;
            _testingTask = Task.Run(() => RunTestingLoopAsync(_cts.Token, schedulingService));
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isTesting) return;

            _cts?.Cancel();
            _isTesting = false;
        }
    }

    private async Task RunTestingLoopAsync(CancellationToken token, ISchedulingService schedulingService)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // Replace this with your actual test logic
                await RunSingleTestStepAsync(token, schedulingService);

                // Optional delay between tests
                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopped
        }
        finally
        {
           _isTesting = false;
        }
    }


    private async Task RunSingleTestStepAsync(CancellationToken token, ISchedulingService schedulingService)
    {
        _schedules = new List<Schedule>();
        for (int i = 0; i < _numberOfSchedulesPerTest; i++)
        {
            _newSchedule.Name = $"Week {_schedules.Count + 1}";

            GenerateRandomConflicticPreferences();

            //Set preferences priority
            int[] preferencesPriority = new int[SchedulingService.NumEmployees];
            if (_schedules.Count > 0)
            {
                preferencesPriority = _schedules.Last().NextPriority;
            }
            else
            {
                preferencesPriority = GetPreferencesPriority(_newSchedule);
            }

            //Apply coef to preferences
            int[,,] preferencesWithCoef = new int[SchedulingService.NumEmployees, SchedulingService.NumDays, SchedulingService.NumShifts];
            for (int e = 0; e < SchedulingService.NumEmployees; e++)
            {
                for (int d = 0; d < SchedulingService.NumDays; d++)
                {
                    for (int s = 0; s < SchedulingService.NumShifts; s++)
                    {
                        preferencesWithCoef[e, d, s] = (_newSchedule.ShiftPreferences[e, d, s] ? 1 : 0) * preferencesPriority[e];
                    }
                }
            }


            //Count total preferences
            int[] numberOfPreferences = new int[SchedulingService.NumEmployees];
            for (int e = 0; e < SchedulingService.NumEmployees; e++)
            {
                int totalPreferences = 0;
                for (int d = 0; d < SchedulingService.NumDays; d++)
                {
                    for (int s = 0; s < SchedulingService.NumShifts; s++)
                    {
                        if (_newSchedule.ShiftPreferences[e, d, s])
                        {
                            totalPreferences++;
                        }
                    }
                }
                numberOfPreferences[e] = totalPreferences;
            }
            _newSchedule.TotalPreferencesCount = numberOfPreferences;

            var beforeTime = DateTime.UtcNow;
            //Create Schedule
            SchedulingResult schedulingResult = schedulingService.Generate(preferencesWithCoef, _newSchedule.UnavailabilityRequests);
            _newSchedule.SchedulingResult = schedulingResult;
            var afterTime = DateTime.UtcNow;
            var diff = afterTime - beforeTime;


            if (schedulingResult.Status != CpSolverStatus.Optimal && schedulingResult.Status != CpSolverStatus.Feasible)
            {
                _schedules.Add(_newSchedule);
                _newSchedule = CreateNewSchedule();
                return;
            }


            //Count unsatisfied preferences
            int[] unsatisfiedPreferencesCount = new int[SchedulingService.NumEmployees];

            for (int e = 0; e < SchedulingService.NumEmployees; e++)
            {
                int unsatisfiedPreferences = 0;
                for (int d = 0; d < SchedulingService.NumDays; d++)
                {
                    for (int s = 0; s < SchedulingService.NumShifts; s++)
                    {
                        if (_newSchedule.SchedulingResult.ShiftsAssingment[e, d, s] == false &&
                            _newSchedule.ShiftPreferences[e, d, s])
                        {
                            unsatisfiedPreferences++;
                        }
                    }
                }
                unsatisfiedPreferencesCount[e] = unsatisfiedPreferences;
            }

            _newSchedule.UnsatisfiedPreferencesCount = unsatisfiedPreferencesCount;

            //Count unsatisfaction percentage 
            int[] unsatisfactionPercentage = new int[SchedulingService.NumEmployees];
            for (int e = 0; e < SchedulingService.NumEmployees; e++)
            {
                if (_newSchedule.TotalPreferencesCount[e] == 0)
                {
                    unsatisfactionPercentage[e] = 0;
                }
                else
                {
                    double var1 = _newSchedule.UnsatisfiedPreferencesCount[e];
                    double var2 = _newSchedule.TotalPreferencesCount[e];
                    unsatisfactionPercentage[e] = (int)(var1 / var2 * 100);
                }
            }
            _newSchedule.UnsatisfactionPercentage = unsatisfactionPercentage;

            //Count unsatisfaction streak
            if (_schedules.Count > 0)
            {
                Schedule previousSchedule = _schedules.Last();
                for (int e = 0; e < SchedulingService.NumEmployees; e++)
                {
                    if (previousSchedule.UnsatisfactionPercentage[e] <= _newSchedule.UnsatisfactionPercentage[e] && previousSchedule.UnsatisfactionPercentage[e] > 0)
                    {
                        _newSchedule.UnsatisfactionStreak[e]++;
                    }
                    else
                    {
                        _newSchedule.UnsatisfactionStreak[e] = 0;
                    }

                    if (_newSchedule.UnsatisfactionStreak[e] > _maxDissatisfactionStreak)
                    {
                        _maxDissatisfactionStreak = _newSchedule.UnsatisfactionStreak[e];
                    }
                }
            }

            //Count future priority
            _newSchedule.NextPriority = GetPreferencesPriority(_newSchedule);


            //Save the schedule
            _schedules.Add(_newSchedule);
            _totalSchedules++;

            //Reset
            _newSchedule = CreateNewSchedule();
        }
        _totalTests++;
    }

    private int[] GetPreferencesPriority(Schedule schedule)
    {
        int[] priorityCoef = new int[SchedulingService.NumEmployees];
        for (int e = 0; e < SchedulingService.NumEmployees; e++)
        {
            priorityCoef[e] = 1;
            if (schedule.UnsatisfactionPercentage[e] != 0)
            {
                priorityCoef[e] *= (int)(schedule.UnsatisfactionPercentage[e] * ((double)SchedulingService.PriorityUnit / 100));
            }
            priorityCoef[e] += (int)(_newSchedule.RolePercentage[e] * ((double)SchedulingService.PriorityUnit / 100));
            priorityCoef[e] += schedule.UnsatisfactionStreak[e] * (SchedulingService.PriorityUnit / 10);
        }

        return priorityCoef;
    }


    private Schedule CreateNewSchedule()
    {
        Schedule schedule = new Schedule();
        schedule.UnsatisfactionStreak = (int[])_newSchedule.UnsatisfactionStreak.Clone();
        return schedule;
    }

    private void GenerateRandomConflicticPreferences()
    {
        var random = new Random();
        Day day = SchedulingService.AllDays[0];
        Shift shift = SchedulingService.AllShifts[0];

        int employeesNumber = random.Next(SchedulingService.NumEmployees);

        for(int e = 0; e < employeesNumber; e++)
        {
            _newSchedule.ShiftPreferences[e, day.Index, shift.Index] = true;
        }
    }

    public int GetTotalSchedules()
    {
        return _totalSchedules;
    }

    public int GetTotalTests()
    {
        return _totalTests;
    }

    public int GetMaxDissatisfactionStreak()
    {
        return _maxDissatisfactionStreak;
    }

}

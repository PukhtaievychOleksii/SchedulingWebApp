using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.OrTools.Sat;
using SchedulingWebApp.Models;
using SchedulingWebApp.Services.Interfaces;
namespace SchedulingWebApp.Services;

public class SchedulingService : ISchedulingService
{
    public static int NumEmployees { get; set; }
    public static int NumDays { get;  set; }
    public static int NumShifts { get; private set; }
    public static List<Employee> AllEmployees { get; set; }
    public static List<Day> AllDays { get;  set; }
    public static List<Shift> AllShifts { get; private set; }
    public static int MaxNumberOfRequirments { get; private set;}
    public static int PriorityUnit { get; private set; }
    public static List<Schedule> Schedules { get; private set; } = new List<Schedule>();

    public static bool[,,] Preferences = new bool[SchedulingService.NumEmployees, SchedulingService.NumDays, SchedulingService.NumShifts];

    public static bool[,,] Unavailability = new bool[SchedulingService.NumEmployees, SchedulingService.NumDays, SchedulingService.NumShifts];

    public static bool KeepRequirments = true;


    private readonly int[,,] schedule;

    public SchedulingService(int numEmployees, int numDays, int numShifts, int maxNumOfRequirments, int priorityUnit) 
    {
        PriorityUnit = priorityUnit;
        NumEmployees = numEmployees;
        NumDays = numDays;
        NumShifts = numShifts;
        AllEmployees = new List<Employee>();

        AllEmployees.Add(new Employee("Josh", Role.Manager, 0));
        AllEmployees.Add(new Employee("Maria", Role.Manager, 1));
        AllEmployees.Add(new Employee("Antony", Role.Employee, 2));
        AllEmployees.Add(new Employee("Viktor", Role.Employee, 3));
        AllEmployees.Add(new Employee("Henry", Role.Employee, 4));
        
        AllDays = new List<Day>();

        AllDays.Add(new Day("Monday", 0));
        AllDays.Add(new Day("Tuesday", 1));
        AllDays.Add(new Day("Wednesday", 2));
        AllDays.Add(new Day("Thursday", 3));
        AllDays.Add(new Day("Friday", 4));
        AllDays.Add(new Day("Saturday", 5));
        AllDays.Add(new Day("Sunday", 6));


        AllShifts = new List<Shift>();
        for(int s = 0; s < numShifts; s++)
        {
            AllShifts.Add(new Shift($"Shift {s + 1}", s));
        }
        MaxNumberOfRequirments = maxNumOfRequirments;
         
    }

    public SchedulingResult Generate(int[,,] prioritizedPreferences, bool[,,] unavailabilityRequest)
    {
        // Creates the model.
        CpModel model = new CpModel();
        model.Model.Variables.Capacity = NumEmployees * NumDays * NumShifts;

        // Creates shift variables.
        // shifts[(e, d, s)]: employee 'e' works shift 's' on day 'd'.
        Dictionary<Tuple<int, int, int>, BoolVar> shifts =
            new Dictionary<Tuple<int, int, int>, BoolVar>(NumEmployees * NumDays * NumShifts);
        for (int e = 0; e < NumEmployees; e++)
        {
            for (int d = 0; d < NumDays; d++)
            {
                for (int s = 0; s < NumShifts; s++)
                {
                    shifts.Add(Tuple.Create(e, d, s), model.NewBoolVar($"shifts_n{e}d{d}s{s}"));
                }
            }
        }

        // Each shift is assigned to exactly one employee in the schedule period.
        List<ILiteral> literals = new List<ILiteral>();
        for (int d = 0; d < NumDays; d++)
        {
            for (int s = 0; s < NumShifts; s++)
            {
                IntVar[] assigned_employees = new IntVar[NumEmployees];
                for (int e = 0; e < NumEmployees; e++)
                {
                    var key = Tuple.Create(e, d, s);
                    assigned_employees[e] = shifts[key];
                }
                model.Add(LinearExpr.Sum(assigned_employees) == 1);
            }
        }



        // Each employee works at most one shift per day.
        for (int e = 0; e < NumEmployees; e++)
        {
            for (int d = 0; d < NumDays; d++)
            {
                IntVar[] assigned_shifts = new IntVar[NumShifts];
                for (int s = 0; s < NumShifts; s++)
                {
                    var key = Tuple.Create(e, d, s);
                    assigned_shifts[s] = shifts[key];
                }
                model.Add(LinearExpr.Sum(assigned_shifts) <= 1);
            }
        }


        // Try to distribute the shifts evenly, so that each employee works minShiftsPerEmployee shifts. 
        int minShiftsPerEmployee = (NumShifts * NumDays) / NumEmployees;
        int maxShiftsPerEmployee;
        if ((NumShifts * NumDays) % NumEmployees == 0)
        {
            maxShiftsPerEmployee = minShiftsPerEmployee;
        }
        else
        {
            maxShiftsPerEmployee = minShiftsPerEmployee + 1;
        }

        for (int e = 0; e < NumEmployees; e++)
        {
            var shiftsWorked = new IntVar[NumDays * NumShifts];
            for (int d = 0; d < NumDays; d++)
            {
                for (int s = 0; s < NumShifts; s++)
                {
                    var key = Tuple.Create(e, d, s);
                    shiftsWorked[d * NumShifts + s] = shifts[key];
                }
            }
            model.AddLinearConstraint(LinearExpr.Sum(shiftsWorked), minShiftsPerEmployee, maxShiftsPerEmployee);
        }


        //Add preferences satisfaction
        IntVar[] flatShifts = new IntVar[NumDays * NumShifts * NumEmployees];
        int[] flatPreferences = new int[NumDays * NumShifts * NumEmployees];
        for (int e = 0; e < NumEmployees; e++)
        {
            for (int d = 0; d < NumDays; d++)
            {
                for (int s = 0; s < NumShifts; s++)
                {
                    var key = Tuple.Create(e, d, s);
                    flatShifts[e * NumDays * NumShifts + d * NumShifts + s] = shifts[key];
                    flatPreferences[e * NumDays * NumShifts + d * NumShifts + s] = prioritizedPreferences[e, d, s];
                }
            }
        }

        model.Maximize(LinearExpr.WeightedSum(flatShifts, flatPreferences));

        //Add request constraints
        flatShifts = new IntVar[NumDays * NumShifts * NumEmployees];
        int[] flatRequests = new int[NumDays * NumShifts * NumEmployees];
        for (int e = 0; e < NumEmployees; e++)
        {
            for (int d = 0; d < NumDays; d++)
            {
                for (int s = 0; s < NumShifts; s++)
                {
                    var key = Tuple.Create(e, d, s);
                    flatShifts[e * NumDays * NumShifts + d * NumShifts + s] = shifts[key];
                    flatRequests[e * NumDays * NumShifts + d * NumShifts + s] = Convert.ToInt32(unavailabilityRequest[e, d, s]);
                }
            }
        }
        model.Add(LinearExpr.WeightedSum(flatShifts, flatRequests) == 0);

        // Solve
        CpSolver solver = new CpSolver();
        CpSolverStatus status = solver.Solve(model);
        Console.WriteLine($"Solve status: {status}");


        //Create schedule 
        bool[,,] schedule = new bool[NumEmployees, NumDays, NumShifts];
        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            for (int e = 0; e < NumEmployees; e++)
            {
                for (int d = 0; d < NumDays; d++)
                {
                    for (int s = 0; s < NumShifts; s++)
                    {
                        var key = Tuple.Create(e, d, s);

                        if (solver.Value(shifts[key]) == 1f)
                        {
                            schedule[e, d, s] = true;
                        }

                        if (solver.Value(shifts[key]) == 0f)
                        {
                            schedule[e, d, s] = false;
                        }
                    }
                }
            }

        }
        return new SchedulingResult(status, schedule);
    }

    public static void SetSchedules(List<Schedule> schedules)
    {
        Schedules = schedules;
    }

    public static void SetPreferences(bool[,,] preferences)
    {
        int dim0 = preferences.GetLength(0);
        int dim1 = preferences.GetLength(1);
        int dim2 = preferences.GetLength(2);

        bool[,,] copy = new bool[dim0, dim1, dim2];

        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                for (int k = 0; k < dim2; k++)
                {
                    copy[i, j, k] = preferences[i, j, k];
                }
            }
        }

        Preferences = copy;
    }

    public static void RestoreToDefault()
    {
        Schedules = new List<Schedule>();

        NumEmployees = 5;
        NumDays = 7;
        NumShifts = 3;
        AllEmployees = new List<Employee>();

        AllEmployees.Add(new Employee("Josh", Role.Manager, 0));
        AllEmployees.Add(new Employee("Maria", Role.Manager, 1));
        AllEmployees.Add(new Employee("Antony", Role.Employee, 2));
        AllEmployees.Add(new Employee("Viktor", Role.Employee, 3));
        AllEmployees.Add(new Employee("Henry", Role.Employee, 4));

        AllDays = new List<Day>();

        AllDays.Add(new Day("Monday", 0));
        AllDays.Add(new Day("Tuesday", 1));
        AllDays.Add(new Day("Wednesday", 2));
        AllDays.Add(new Day("Thursday", 3));
        AllDays.Add(new Day("Friday", 4));
        AllDays.Add(new Day("Saturday", 5));
        AllDays.Add(new Day("Sunday", 6));


        AllShifts = new List<Shift>();
        for (int s = 0; s < NumShifts; s++)
        {
            AllShifts.Add(new Shift($"Shift {s + 1}", s));
        }
    }
}

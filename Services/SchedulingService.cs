using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.OrTools.Sat;
using SchedulingWebApp.Models;
namespace SchedulingWebApp.Services;

public class SchedulingService : ISchedulingService
{
    public static int NumEmployees { get; private set; }
    public static int NumDays { get; private set; }
    public static int NumShifts { get; private set; }
    public static int[] AllEmployees { get; private set; }
    public static int[] AllDays { get; private set; }
    public static int[] AllShifts { get; private set; }
    public static int MaxNumberOfRequirments { get; private set;}


    private readonly int[,,] schedule;

    public SchedulingService(int numEmployees, int numDays, int numShifts, int maxNumOfRequirments) 
    {
        NumEmployees = numEmployees;
        NumDays = numDays;
        NumShifts = numShifts;
        AllEmployees = Enumerable.Range(0, NumEmployees).ToArray();
        AllDays = Enumerable.Range(0, NumDays).ToArray();
        AllShifts = Enumerable.Range(0, NumShifts).ToArray();
        MaxNumberOfRequirments = maxNumOfRequirments;
         
    }

    public SchedulingResult Generate(int[,,] prioritizedPreferences, bool[,,] unavailabilityRequest)
    {
        // Creates the model.
        CpModel model = new CpModel();
        model.Model.Variables.Capacity = NumEmployees * NumDays * NumShifts;

        // Creates shift variables.
        // shifts[(n, d, s)]: employee 'e' works shift 's' on day 'd'.
        Dictionary<Tuple<int, int, int>, BoolVar> shifts =
            new Dictionary<Tuple<int, int, int>, BoolVar>(NumEmployees * NumDays * NumShifts);
        foreach (int e in AllEmployees)
        {
            foreach (int d in AllDays)
            {
                foreach (int s in AllShifts)
                {
                    shifts.Add(Tuple.Create(e, d, s), model.NewBoolVar($"shifts_n{e}d{d}s{s}"));
                }
            }
        }

        // Each shift is assigned to exactly one nurse in the schedule period.
        List<ILiteral> literals = new List<ILiteral>();
        foreach (int d in AllDays)
        {
            foreach (int s in AllShifts)
            {
                IntVar[] assigned_employees = new IntVar[NumEmployees];
                foreach (int e in AllEmployees)
                {
                    var key = Tuple.Create(e, d, s);
                    assigned_employees[e] = shifts[key];
                }
                model.Add(LinearExpr.Sum(assigned_employees) == 1);
            }
        }


        // Each employee works at most one shift per day.
        foreach (int e in AllEmployees)
        {
            foreach (int d in AllDays)
            {
                IntVar[] assigned_shifts = new IntVar[NumShifts];
                foreach (int s in AllShifts)
                {
                    var key = Tuple.Create(e, d, s);
                    assigned_shifts[s] = shifts[key];
                }
                model.Add(LinearExpr.Sum(assigned_shifts) <= 1);
            }
        }

        // Try to distribute the shifts evenly, so that each employee works
        // minShiftsPerEmployee shifts. If this is not possible, because the total
        // number of shifts is not divisible by the number of employees, some employees will
        // be assigned one more shift.
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

        foreach (int e in AllEmployees)
        {
            var shiftsWorked = new IntVar[NumDays * NumShifts];
            foreach (int d in AllDays)
            {
                foreach (int s in AllShifts)
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
        foreach (int e in AllEmployees)
        {
            foreach (int d in AllDays)
            {
                foreach (int s in AllShifts)
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
        foreach (int e in AllEmployees)
        {
            foreach (int d in AllDays)
            {
                foreach (int s in AllShifts)
                {
                    var key = Tuple.Create(e, d, s);
                    flatShifts[e * NumDays * NumShifts + d * NumShifts + s] = shifts[key];
                    flatRequests[e * NumDays * NumShifts + d * NumShifts + s] = Convert.ToInt32(unavailabilityRequest[e,d,s]);
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
            foreach (int e in AllEmployees)
            {
                foreach (int d in AllDays)
                {
                    foreach (int s in AllShifts)
                    {
                        var key = Tuple.Create(e, d, s);

                        if (solver.Value(shifts[key]) == 1f)
                        {
                            schedule[e, d, s] = true;
                        }

                        if (solver.Value(shifts[key]) == 0F)
                        {
                            schedule[e, d, s] = false;
                        }
                    }
                }
            }
        }
        return new SchedulingResult(status, schedule);
    }
}

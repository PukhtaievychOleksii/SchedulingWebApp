using Google.OrTools.Sat;

namespace SchedulingWebApp.Models;

public struct SchedulingResult
{
    public CpSolverStatus Status { get; private set; }
    public bool[,,] ShiftsAssingment { get; private set; }

    //public string SchedulePrintout { get; private set; }

    public SchedulingResult(CpSolverStatus status, bool[,,] schedule)
    {
        Status = status;
        ShiftsAssingment = schedule;
    }
}

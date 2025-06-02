using SchedulingWebApp.Models;

namespace SchedulingWebApp.Services.Interfaces;

public interface IPriorityAnalysysService
{
    string Evaluate(Employee emp1, Employee emp2, Schedule schedule);
}

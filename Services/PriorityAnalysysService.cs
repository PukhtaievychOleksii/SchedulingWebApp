using Radzen.Blazor;
using SchedulingWebApp.Models;
using SchedulingWebApp.Services.Interfaces;

namespace SchedulingWebApp.Services;

public class PriorityAnalysysService : IPriorityAnalysysService
{
    public string Evaluate(Employee emp1, Employee emp2, Schedule schedule)
    {
        string emp1Name = emp1.Name;
        string emp1Role = emp1.Role.ToString();
        int unsatisfactionPercentage1 = schedule.UnsatisfactionPercentage[emp1.Index];
        int rolePercentage1 = schedule.RolePercentage[emp1.Index];
        int unsatisfactionStreak1 = schedule.UnsatisfactionStreak[emp1.Index];
        int totalPriority1 = schedule.NextPriority[emp1.Index];

        string emp2Name = emp2.Name;
        string emp2Role = emp2.Role.ToString();
        int unsatisfactionPercentage2 = schedule.UnsatisfactionPercentage[emp2.Index];
        int rolePercentage2 = schedule.RolePercentage[emp2.Index];
        int unsatisfactionStreak2 = schedule.UnsatisfactionStreak[emp2.Index];
        int totalPriority2 = schedule.NextPriority[emp2.Index];

        string higherEmp = totalPriority1 > totalPriority2 ? emp1Name : emp2Name;
        string lowerEmp = totalPriority1 > totalPriority2 ? emp2Name : emp1Name;

        var reasons = new List<string>();

        if (unsatisfactionPercentage1 != unsatisfactionPercentage2)
        {
            if ((totalPriority1 > totalPriority2 && unsatisfactionPercentage1 > unsatisfactionPercentage2) ||
                (totalPriority2 > totalPriority1 && unsatisfactionPercentage2 > unsatisfactionPercentage1))
            {
                reasons.Add($"{higherEmp} experienced a higher percentage of unmet preferences during the last planning horizon.");
            }
        }
        else if (unsatisfactionPercentage1 != 0)
        {
            reasons.Add($"Both employees had an equal level of unmet preferences ({unsatisfactionPercentage1}%) during the last planning horizon.");
        }

        if (rolePercentage1 != rolePercentage2)
        {
            if ((totalPriority1 > totalPriority2 && rolePercentage1 > rolePercentage2) ||
                (totalPriority2 > totalPriority1 && rolePercentage2 > rolePercentage1))
            {
                reasons.Add($"{higherEmp} holds a higher role in the company ({(higherEmp == emp1Name ? emp1Role : emp2Role)}).");
            }
        }
        else if (rolePercentage1 != 0)
        {
            reasons.Add("Both employees hold roles of equal weight in the company.");
        }

        if (unsatisfactionStreak1 != unsatisfactionStreak2)
        {
            if ((totalPriority1 > totalPriority2 && unsatisfactionStreak1 > unsatisfactionStreak2) ||
                (totalPriority2 > totalPriority1 && unsatisfactionStreak2 > unsatisfactionStreak1))
            {
                reasons.Add($"{higherEmp} had a longer streak of unmet preferences.");
            }
        }
        else if (unsatisfactionStreak1 != 0)
        {
            reasons.Add("Both employees had equally long streaks of unsatisfied preferences.");
        }

        foreach(ScheduleParameter parameter in Schedule.GlobalScheduleParameters.Keys)
        {
            int value1 = Schedule.GlobalScheduleParameters[parameter][emp1.Index];
            int value2 = Schedule.GlobalScheduleParameters[parameter][emp2.Index];
            if (value1 != value2)
            {
                if ((totalPriority1 > totalPriority2 && value1 > value2) || (totalPriority2 > totalPriority1 && value2 > value1))
                {
                    if (parameter.MinValue >= 0 && parameter.MaxValue >= 0)
                    {
                        reasons.Add($"{higherEmp} had a higher {parameter.Name} during the last planning horizon.");
                    }
                    if(parameter.MinValue <= 0 && parameter.MaxValue <= 0)
                    {
                        reasons.Add($"{higherEmp} had a smaller {parameter.Name} during the last planning horizon.");
                    }
                }
            } else
            {
                reasons.Add($"Both employees had equal {parameter.Name} during the last planning horizon.");
            }
        }

        if (reasons.Count == 0 || totalPriority1 == totalPriority2)
        {
            reasons.Add("These factors resulted in both employees' preferences being equally prioritized; therefore, a randomized selection was used to ensure fairness in the final decision.");
        }
        else
        {
            reasons.Add($"This factor(s) resulted in {higherEmp}'s preferences outweighting {lowerEmp}'s preferences.");
        }

        string explanation = string.Join(" ", reasons);
        //TODO:fininsh for cases when the same amount of satisfied preferences 
        return explanation;
    }

}


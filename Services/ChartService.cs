using Radzen.Blazor;
using SchedulingWebApp.Services.Interfaces;

namespace SchedulingWebApp.Services
{
    public class ChartService: IChartService
    {
        public static List<string> ChartTypes = new List<string>() { 
            "Unsatisfaction percentage",
            "Unsatisfaction streak"
        };
    }
}

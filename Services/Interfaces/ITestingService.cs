namespace SchedulingWebApp.Services.Interfaces;

public interface ITestingService
{
    public void Start(ISchedulingService schedulingService, int schedulesPerTest);
    public void Stop();
    public int GetTotalSchedules();
    public int GetTotalTests();

    public int GetMaxDissatisfactionStreak();

}

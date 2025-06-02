using SchedulingWebApp.Components;
using SchedulingWebApp.Services;
using SchedulingWebApp.Services.Interfaces;

namespace SchedulingWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            int numOfEmployees = 5;
            int numOfDays = 7;
            int numOfShifts = 3;
            int maxNumOfRequirments = 3;
            int priorityUnit = 100;
            int schedulesInHistory = 4;
            string apiKey = "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";
            SchedulingService schedulingService = new SchedulingService(numOfEmployees, numOfDays, numOfShifts, maxNumOfRequirments, priorityUnit);
            OpenAIClient openAIClient = new OpenAIClient(apiKey, schedulesInHistory);

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddScoped<ISchedulingService>(factory => schedulingService);

            builder.Services.AddScoped<IAIService>(factory => openAIClient);

            builder.Services.AddScoped<IPriorityAnalysysService, PriorityAnalysysService>();

            builder.Services.AddScoped<IChartService, ChartService>();

            builder.Services.AddScoped<Radzen.TooltipService>();

            builder.Services.AddScoped<Radzen.DialogService>();

            builder.Services.AddScoped<ITestingService, TestingService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}

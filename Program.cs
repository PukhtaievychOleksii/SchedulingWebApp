using SchedulingWebApp.Components;
using SchedulingWebApp.Services;

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
            SchedulingService schedulingService = new SchedulingService(numOfEmployees, numOfDays, numOfShifts, maxNumOfRequirments);

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddScoped<ISchedulingService>(factory => schedulingService);
            
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

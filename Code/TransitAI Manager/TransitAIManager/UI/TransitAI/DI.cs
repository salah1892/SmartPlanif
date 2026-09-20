using Core.Application;
using TransitAI.Components.LogicMetier;
using TransitAI.Services.MenuServices;
using BlazorLeaflet;
using TransitAI.Components.Pages.Dashboard.Models;
using App.Infrastructure;
namespace TransitAI
{
    public static class DI
    {
        public static IServiceCollection AddDIServices(this IServiceCollection services, IConfiguration configuration)
        {
            //>>> Application Layer
            services.AddApplicationServices();

           // Infrastructure Layer
            services.AddInfrastructureServices(configuration);
            services.AddScoped<NotificationDrawerState>();
            services.AddSingleton<DashboardStateService>();
            services.AddScoped<MenuStateService>();
        
            // Ajout de BlazorLeaflet
           // services.AddBlazorLeaflet();
        
        
            return services;
        }
    }
}
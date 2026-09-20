using App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable;
using App.Infrastructure.DataAccessManager.EFCore.Contexts;
using App.Infrastructure.DataAccessManager.EFCore.Repositories;
using App.Infrastructure.Services.DrAgentServices;
using App.Infrastructure.Services.DrDelegationServices;
using App.Infrastructure.Services.DrDepartServices;
using App.Infrastructure.Services.DrLignesServices;
using App.Infrastructure.Services.DrStation;
using App.Infrastructure.Services.DrVehiculeServices;
using Core.Application.Common.CQS.Commands;
using Core.Application.Common.CQS.Queries;
using Core.Application.Common.Repositories;
using Core.Application.Services.DrAgentServices;
using Core.Application.Services.DrDelegationServices;
using Core.Application.Services.DrDepartServices;
using Core.Application.Services.DrLignesServices;
using Core.Application.Services.DrStationServices;
using Core.Application.Services.DrVehiculeServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using QueryContext = App.Infrastructure.DataAccessManager.EFCore.Contexts.QueryContext;

namespace App.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var databaseProvider = configuration["DatabaseProvider"];

            // Register Context
            switch (databaseProvider)
            {
                case "MySql":
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    services.AddDbContext<DataContext>(options =>
                        //options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 2)))
                        options.UseMySql(connectionString, serverVersion)
                            .LogTo(Log.Information, LogLevel.Information)
                            .EnableSensitiveDataLogging()
                    );
                    services.AddDbContext<CommandContext>(options =>
                        //options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 2)))
                        options.UseMySql(connectionString, serverVersion)
                            .LogTo(Log.Information, LogLevel.Information)
                            .EnableSensitiveDataLogging()
                    );
                    services.AddDbContext<QueryContext>(options =>
                        //options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 2)))
                        options.UseMySql(connectionString, serverVersion)
                            .LogTo(Log.Information, LogLevel.Information)
                            .EnableSensitiveDataLogging()
                    );
                    break;
                // default:
                //     throw new InvalidOperationException(
                //         $"Database provider '{databaseProvider}' not supported. Check your appsettings.json");
                //case "SqlServer":
                //default:
                //    services.AddDbContext<DataContext>(options =>
                //        options.UseSqlServer(connectionString)
                //        .LogTo(Log.Information, LogLevel.Information)
                //        .EnableSensitiveDataLogging()
                //    );
                //    services.AddDbContext<CommandContext>(options =>
                //        options.UseSqlServer(connectionString)
                //        .LogTo(Log.Information, LogLevel.Information)
                //        .EnableSensitiveDataLogging()
                //    );
                //    services.AddDbContext<QueryContext>(options =>
                //        options.UseSqlServer(connectionString)
                //        .LogTo(Log.Information, LogLevel.Information)
                //        .EnableSensitiveDataLogging()
                //    );
                //    break;
            }

            //services.AddDbContext<DataContext>();
            services.AddScoped<ICommandContext, CommandContext>();
            services.AddScoped<IQueryContext, QueryContext>();

            //services.AddScoped<ICommandContext>(sp => sp.GetRequiredService<CommandContext>());
            //services.AddScoped<IQueryContext>(sp => sp.GetRequiredService<QueryContext>());

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));
            //ModelServices
            services.AddScoped<IDrAgentService, DrAgentService>();
            services.AddScoped<IDrLignesServices, DrLignesServices>();
            services.AddScoped<IDrStationServices, DrStationServices>();
            services.AddScoped<IDrDelegationService, DrDelegationService>();
            services.AddScoped<IDrVehiculeService, DrVehiculeService>();
            services.AddScoped<IDrDepartService, DrDepartService>();
            return services;
        }
    }
}
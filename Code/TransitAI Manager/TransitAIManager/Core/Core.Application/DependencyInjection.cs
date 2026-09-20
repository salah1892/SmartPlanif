using System.Reflection;
using Core.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
namespace Core.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {        //>>> Common
            // services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            // services.AddScoped<IUnitOfWork, UnitOfWork>();
            //>>> AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            //>>> FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            //>>> MediatR
            services.AddMediatR(x =>
            {
                x.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
                x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            });
            return services;
        }
    }
}
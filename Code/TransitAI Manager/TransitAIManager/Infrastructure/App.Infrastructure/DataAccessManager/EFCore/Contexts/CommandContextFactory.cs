using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.DataAccessManager.EFCore.Contexts
{
    public class CommandContextFactory: IDesignTimeDbContextFactory<CommandContext>
    {
        public CommandContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory());
        
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var builder = new DbContextOptionsBuilder<CommandContext>();
            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new CommandContext(builder.Options);
        }
    }
}
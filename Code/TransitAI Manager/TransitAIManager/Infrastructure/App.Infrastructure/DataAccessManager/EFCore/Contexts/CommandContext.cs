using Core.Application.Common.CQS.Commands;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.DataAccessManager.EFCore.Contexts
{
    public class CommandContext : DataContext, ICommandContext
    {
        public CommandContext(DbContextOptions<CommandContext> options)
            : base(options)
        {
        }
    }
}
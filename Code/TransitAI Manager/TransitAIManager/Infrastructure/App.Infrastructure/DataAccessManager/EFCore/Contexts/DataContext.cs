using App.Infrastructure.DataAccessManager.EFCore.Configurations.AspNetIdentity;
using App.Infrastructure.DataAccessManager.EFCore.Configurations.ConfigurationTable;
using Core.Application.Common.Repositories;
using Core.Domain.Entities.Identity;
using Core.Domain.Entities.Metier;
using Core.Domain.Identity.AspNetIdentity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.DataAccessManager.EFCore.Contexts
{
    public class DataContext : IdentityDbContext<ApplicationUser>, IEntityDbSet
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        protected DataContext(DbContextOptions options) : base(options) { }
        // Tables AspNetIdentity
        public DbSet<Token> Token { get; set; }

        // Tables Métier & Référentiel
        public DbSet<DrAgent> DrAgent { get; set; }
        public DbSet<DrCatVe> DrCatVe { get; set; } // <-- Ajouté
        public DbSet<DrDeleg> DrDeleg { get; set; } // <-- Ajouté
        public DbSet<DrDentr> DrDentr { get; set; } // <-- Ajouté (Dépôts/Entreprises)
        public DbSet<DrDepar> DrDepar { get; set; }
        public DbSet<DrItin> DrItin { get; set; }
        public DbSet<DrStati> DrStati { get; set; }
        public DbSet<DrLigne> DrLigne { get; set; }
        public DbSet<DrTyLi> DrTyLi { get; set; } // <-- Ajouté
        public DbSet<DrVehic> DrVehic  { get; set; }
    
        // Tables GTFS
        public DbSet<Trip> Trips { get; set; }
        public DbSet<StopTime> Stop_Times { get; set; }
        public DbSet<FareAttribute> Fare_Attributes { get; set; }
        public DbSet<FareRule> Fare_Rules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Appliquer automatiquement toutes les configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);

        
            modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
            modelBuilder.ApplyConfiguration(new TokenConfiguration());
        
            modelBuilder.ApplyConfiguration(new DrAgentConfiguration());
            modelBuilder.ApplyConfiguration(new DrDeparConfiguration());
            modelBuilder.ApplyConfiguration(new DrItinConfiguration());
            modelBuilder.ApplyConfiguration(new DrStationConfiguration());
            modelBuilder.ApplyConfiguration(new DrLigneConfiguration());
            modelBuilder.ApplyConfiguration(new DrTyLiConfiguration());
            modelBuilder.ApplyConfiguration(new FareRuleConfiguration());
            modelBuilder.ApplyConfiguration(new StopTimeConfiguration());
            modelBuilder.ApplyConfiguration(new TripConfiguration());
            modelBuilder.ApplyConfiguration(new DrVehiculeConfiguration());
            modelBuilder.ApplyConfiguration(new DrCatVeConfiguration());
        }
    }
}
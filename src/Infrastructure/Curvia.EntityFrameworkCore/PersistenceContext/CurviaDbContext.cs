using Curvia.Domain.Features.Routing.Routes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.PersistenceContext;

namespace Curvia.Persistence.EntityFrameworkCore.PersistenceContext;

public class CurviaDbContext : TemplatesCoreDbContextBase
{
	#region Properties
	protected readonly string _connectionString = default!;
	protected readonly bool _configureDbContextManuallyFromBuildInConnectionString = false;
	protected readonly bool _configureDbContextManuallyFromSuppliedConnectionString = false;
	#endregion

	#region Constructor
	public CurviaDbContext(DbContextOptions<CurviaDbContext> dbContextOptions)
		:base(dbContextOptions)
	{
		
	}
	#endregion

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (_configureDbContextManuallyFromBuildInConnectionString)
		{
			// when not configured from API, use this one.
			optionsBuilder.UseSqlServer($"Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=AbaaDb_{DateTime.UtcNow.ToShortDateString}")
				// add logging of SQL-command execution to console.
				.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
				// enabling sensitive logging will add some details like parameter values of execution commands.
				.EnableSensitiveDataLogging();
		}
		else if (_configureDbContextManuallyFromSuppliedConnectionString)
		{
			optionsBuilder.UseSqlServer(_connectionString);
		}
		;

		base.OnConfiguring(optionsBuilder);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CurviaDbContext).Assembly);

	}
}

using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.PersistenceContext;

namespace Curvia.Persistence.EntityFrameworkCore.PersistenceContext;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core DbContext for Curvia.
///              Inherits common configuration/behavior from <see cref="TemplatesCoreDbContextBase"/> and
///              supports optional manual configuration modes (LocalDB or supplied connection string).
///              Also applies all entity configurations from the current assembly.
/// </summary>
public class CurviaDbContext : TemplatesCoreDbContextBase
{
	#region Properties
	/// <summary>
	/// Optional supplied connection string used when manual configuration is enabled.
	/// </summary>
	protected readonly string _connectionString = default!;

	/// <summary>
	/// Enables manual configuration using a built-in LocalDB connection string.
	/// </summary>
	protected readonly bool _configureDbContextManuallyFromBuildInConnectionString = false;

	/// <summary>
	/// Enables manual configuration using the value stored in <see cref="_connectionString"/>.
	/// </summary>
	protected readonly bool _configureDbContextManuallyFromSuppliedConnectionString = false;
	#endregion

	#region Constructor
	/// <summary>
	/// Creates a new <see cref="CurviaDbContext"/> instance using DI-provided options.
	/// </summary>
	/// <param name="dbContextOptions">EF Core options for this DbContext.</param>
	public CurviaDbContext(DbContextOptions<CurviaDbContext> dbContextOptions)
		: base(dbContextOptions)
	{

	}
	#endregion

	#region Overrides
	/// <summary>
	/// Configures EF Core model metadata for Curvia and applies all IEntityTypeConfiguration
	/// implementations found in the current assembly.
	/// </summary>
	/// <param name="modelBuilder">The model builder used to configure entities.</param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CurviaDbContext).Assembly);
	}

	/// <summary>
	/// Optionally configures the DbContext when the host did not configure it via DI.
	/// Supports:
	/// - Built-in LocalDB connection string with SQL command logging enabled
	/// - Supplied connection string stored in <see cref="_connectionString"/>
	/// </summary>
	/// <param name="optionsBuilder">Options builder to configure the DbContext.</param>
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
	#endregion
}
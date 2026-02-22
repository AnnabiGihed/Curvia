using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Tools.DependencyInjection;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Templates.Core.Tools.DependencyInjection.Abstractions;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Templates.Core.Infrastructure.Abstraction.Transaction;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Curvia.Persistence.EntityFrameworkCore.Features.Users.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Motorcycles.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Repositories;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Transaction;
using Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers Curvia persistence infrastructure into the DI container.
///              This installer:
///              - Optionally applies convention-based registrations for the persistence assembly
///              - Registers <see cref="CurviaDbContext"/> with SQL Server using the configured connection string
///              - Registers the transaction manager implementation for EF Core
///              - Registers repository implementations for Users, Motorcycles, Saved Routes and Motorcycle Catalog
/// </summary>
public sealed class PersistanceServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	#region Public Methods
	/// <summary>
	/// Registers persistence-related services into the DI container.
	/// </summary>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">Application configuration used to resolve connection strings and settings.</param>
	/// <param name="includeConventionBasedRegistration">When true, also includes convention-based registrations for the persistence assembly.</param>
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
		{
			base.IncludeConventionBasedRegistrations(services, configuration, new List<Assembly>() { AssemblyReference.Assembly }.ToArray());
		}

		#region DbContext
		services.AddDbContext<CurviaDbContext>(options => options.UseSqlServer(configuration.GetConnectionString($"{nameof(CurviaDbContext)}ConnectionString")));
		#endregion

		#region Transaction
		services.AddScoped(typeof(ITransactionManager<CurviaDbContext>), typeof(TransactionManager<CurviaDbContext>));
		#endregion

		#region Users & Motorcycles
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IMotorcycleRepository, MotorcycleRepository>();
		#endregion

		#region Saved Routes
		services.AddScoped<ISavedRouteRepository, SavedRouteRepository>();
		#endregion

		#region Motorcycle Catalog
		services.AddScoped<IMotorcycleMakerRepository, MotorcycleMakerRepository>();
		services.AddScoped<IMotorcycleCatalogModelRepository, MotorcycleCatalogModelRepository>();
		#endregion
	}
	#endregion
}
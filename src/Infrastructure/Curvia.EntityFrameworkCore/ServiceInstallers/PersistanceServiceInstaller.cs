using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Motorcycles.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Users.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Templates.Core.Infrastructure.Abstraction.Transaction;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Transaction;
using Templates.Core.Tools.DependencyInjection;
using Templates.Core.Tools.DependencyInjection.Abstractions;

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

		#region Awareness
		services.AddScoped<IHazardRepository, HazardRepository>();
		services.AddScoped<IRoadWorkRepository, RoadWorkRepository>();
		services.AddScoped<IIncidentRepository, IncidentRepository>();
		services.AddScoped<ISpeedCameraRepository, SpeedCameraRepository>();
		#endregion
	}
	#endregion
}
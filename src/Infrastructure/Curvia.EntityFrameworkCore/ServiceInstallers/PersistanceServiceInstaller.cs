using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Domain.Features.Routing.Routes.Repositories;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Motorcycles.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.Users.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Curvia.Persistence.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.DomainEventPublisher;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.Repositories;
using Pivot.Framework.Infrastructure.Abstraction.Transaction;
using Pivot.Framework.Infrastructure.Abstraction.UnitOfWork;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Publisher;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Outbox.Repositories;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Transaction;
using Pivot.Framework.Tools.DependencyInjection;
using Pivot.Framework.Tools.DependencyInjection.Abstractions;
using System.Reflection;

namespace Curvia.Persistence.EntityFrameworkCore.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers Curvia persistence infrastructure into the DI container.
///              This installer:
///              - Optionally applies convention-based registrations for the persistence assembly
///              - Registers CurviaDbContext with SQL Server using the configured connection string
///              - Registers the outbox stack: IOutboxRepository + IDomainEventPublisher
///                (both are generic classes — Scrutor skips them, so they must be explicit)
///              - Registers IUnitOfWork&lt;CurviaDbContext&gt; → CurviaUnitOfWork
///              - Registers the transaction manager implementation for EF Core
///              - Registers repository implementations for all features
/// </summary>
public sealed class PersistanceServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
		{
			base.IncludeConventionBasedRegistrations(services, configuration, new List<Assembly>() { AssemblyReference.Assembly }.ToArray());
		}

		#region DbContext
		services.AddDbContext<CurviaDbContext>(options => options.UseSqlServer(configuration.GetConnectionString($"{nameof(CurviaDbContext)}ConnectionString")));
		#endregion

		#region Outbox
		// Generic classes are excluded by Scrutor (ContainsGenericParameters filter), so these must be explicit.
		// OutboxRepository<CurviaDbContext> is required by DomainEventPublisher<CurviaDbContext>.
		// DomainEventPublisher<CurviaDbContext> is required by CurviaUnitOfWork.
		services.AddScoped<IOutboxRepository<CurviaDbContext>, OutboxRepository<CurviaDbContext>>();
		services.AddScoped<IDomainEventPublisher, DomainEventPublisher<CurviaDbContext>>();
		#endregion

		#region Unit of Work
		services.AddScoped<IUnitOfWork<CurviaDbContext>, CurviaUnitOfWork>();
		// Forward the non-generic IUnitOfWork (used by Application layer handlers) to the
		// already-registered scoped CurviaUnitOfWork — no second instance is created.
		services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IUnitOfWork<CurviaDbContext>>());
		#endregion

		#region Transaction
		services.AddScoped(typeof(ITransactionManager<CurviaDbContext>), typeof(TransactionManager<CurviaDbContext>));
		#endregion

		#region Routing
		services.AddScoped<IRouteRepository, RouteRepository>();
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
}
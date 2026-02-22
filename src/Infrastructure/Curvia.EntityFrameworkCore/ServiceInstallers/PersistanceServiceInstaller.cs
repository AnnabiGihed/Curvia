using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Curvia.Domain.Features.Users.Repositories;
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

		#region Transaction
		services.AddScoped(typeof(ITransactionManager<CurviaDbContext>), typeof(TransactionManager<CurviaDbContext>));
		#endregion

		#region Users & Saved Routes
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IMotorcycleRepository, MotorcycleRepository>();
		services.AddScoped<ISavedRouteRepository, SavedRouteRepository>();
		#endregion
	}
}
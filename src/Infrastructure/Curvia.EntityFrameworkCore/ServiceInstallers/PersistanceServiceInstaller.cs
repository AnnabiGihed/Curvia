using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Tools.DependencyInjection;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Domain.Features.SavedRoutes.Repositories;
using Templates.Core.Infrastructure.Abstraction.Transaction;
using Templates.Core.Tools.DependencyInjection.Abstractions;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Curvia.Persistence.EntityFrameworkCore.Features.Users.Repositories;
using Curvia.Persistence.EntityFrameworkCore.Features.SavedRoutes.Repositories;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Transaction;

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
		services.AddScoped<ISavedRouteRepository, SavedRouteRepository>();
		#endregion
	}
}
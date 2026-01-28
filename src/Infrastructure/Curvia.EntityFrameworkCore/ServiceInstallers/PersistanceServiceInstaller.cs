using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
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
	}
}
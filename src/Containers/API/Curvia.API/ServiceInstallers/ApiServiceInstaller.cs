using System.Reflection;
using Templates.Core.Tools.DependencyInjection;
using Templates.Core.Tools.DependencyInjection.Abstractions;

namespace Curvia.API.ServiceInstallers;

public sealed class ApiServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
		{
			base.IncludeConventionBasedRegistrations(services, configuration, new List<Assembly>() { AssemblyReference.Assembly }.ToArray());
		}
	}
}

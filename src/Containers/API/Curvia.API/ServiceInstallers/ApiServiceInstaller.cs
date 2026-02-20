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

		#region CORS
		// "Open" policy referenced in HostingExtensions.ConfigurePipeline().
		// Restrict AllowedOrigins in production.
		services.AddCors(options =>
		{
			options.AddPolicy("Open", policy =>
				policy.AllowAnyOrigin()
					  .AllowAnyHeader()
					  .AllowAnyMethod());
		});
		#endregion

		#region Auth stubs
		// Keycloak integration comes in V2.
		services.AddAuthentication();
		services.AddAuthorization();
		#endregion
	}
}

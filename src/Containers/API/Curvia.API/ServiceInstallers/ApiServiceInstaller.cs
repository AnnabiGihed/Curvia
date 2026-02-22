using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
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

		// Default authorization policy for all endpoints
		var requireAuthenticatedUserPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

		// Configure controllers with default authorization filter
		services.AddControllers(configure =>
		{
			configure.Filters.Add(new AuthorizeFilter(requireAuthenticatedUserPolicy));
		})
		.AddJsonOptions(options =>
		{
			options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
		});


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
		services.AddKeycloakAuthentication(configuration);
		#endregion
	}
}
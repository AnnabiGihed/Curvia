using System.Reflection;
using System.Text.Json.Serialization;
using Curvia.API.Middleware.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Pivot.Framework.Tools.DependencyInjection;
using Pivot.Framework.Tools.DependencyInjection.Abstractions;

namespace Curvia.API.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers API-layer services into the DI container.
///              This installer:
///              - Optionally applies convention-based registrations for the API assembly
///              - Configures MVC controllers with a global authenticated-user authorization filter
///              - Adds JSON options (string enums)
///              - Registers CORS policy "Open" (intended for dev; restrict in production)
///              - Registers Keycloak authentication and the JIT provisioning user context
/// </summary>
public sealed class ApiServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	#region Public Methods
	/// <summary>
	/// Registers API-related services into the DI container.
	/// </summary>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">Application configuration used to resolve settings.</param>
	/// <param name="includeConventionBasedRegistration">When true, also includes convention-based registrations for the API assembly.</param>
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
		{
			base.IncludeConventionBasedRegistrations(services, configuration, new List<Assembly>() { AssemblyReference.Assembly }.ToArray());
		}

		var requireAuthenticatedUserPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

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

		#region JIT Provisioning Context
		services.AddScoped<IAppUserContext, AppUserContext>();
		#endregion
	}
	#endregion
}
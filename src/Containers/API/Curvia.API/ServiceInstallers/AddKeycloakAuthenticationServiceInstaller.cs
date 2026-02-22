using System.Security.Claims;
using Templates.Core.Caching.Extensions;
using Templates.Core.Authentication.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Curvia.API.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Wires Keycloak JWT + Redis token caching + Swagger OAuth into DI.
/// </summary>
public static class AddKeycloakAuthenticationServiceInstaller
{
	#region Public Methods
	/// <summary>
	/// Registers Keycloak authentication support, including:
	/// - Redis caching used for token-related operations
	/// - JwtBearer role claim type configuration (<see cref="ClaimTypes.Role"/>)
	/// - Swagger/OpenAPI configuration with Keycloak security definition and requirement
	/// </summary>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">Application configuration.</param>
	public static void AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddKeycloakRedisCache(configuration);

		services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
		{
			options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
		});

		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new() { Title = "Curvia API", Version = "v1" });
			c.AddKeycloakSecurityDefinition(services.BuildServiceProvider());
			c.AddKeycloakSecurityRequirement();
		});
	}
	#endregion
}
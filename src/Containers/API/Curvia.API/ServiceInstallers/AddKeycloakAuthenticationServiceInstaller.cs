using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Pivot.Framework.Authentication.Caching.Extensions;
using Pivot.Framework.Authentication.AspNetCore.Extensions;

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
		services.AddKeycloakAuthentication(configuration, o => o
			.WithCurrentUser()
			.WithRedisTokenCaching()
			.WithSwagger("Curvia API", "v1"));

		services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
		{
			options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
		});
	}
	#endregion
}
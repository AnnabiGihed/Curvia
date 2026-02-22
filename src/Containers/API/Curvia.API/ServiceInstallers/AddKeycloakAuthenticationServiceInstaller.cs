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
	public static void AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		// 1. Full Keycloak + Redis stack.
		//    Internally calls AddAuthentication(Bearer) and AddAuthorization() — do NOT call again.
		services.AddKeycloakRedisCache(configuration);

		// 2. Fix the RoleClaimType mismatch introduced by AddKeycloakRedisCache.
		//    PostConfigure runs after all Configure calls, so this wins regardless of order.
		services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
		{
			options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
		});

		// 3. Swagger with Keycloak OAuth2 PKCE.
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new() { Title = "Curvia API", Version = "v1" });
			c.AddKeycloakSecurityDefinition(services.BuildServiceProvider());
			c.AddKeycloakSecurityRequirement();
		});
	}
}
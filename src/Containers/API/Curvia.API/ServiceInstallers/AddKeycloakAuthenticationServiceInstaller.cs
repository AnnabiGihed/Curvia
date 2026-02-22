using Templates.Core.Caching.Extensions;
using Templates.Core.Authentication.Extensions;

namespace Curvia.API.ServiceInstallers;

public static class AddKeycloakAuthenticationServiceInstaller
{
	public static void AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		#region 1. Keycloak + Redis (replaces AddKeycloakBackend alone)
		//   - Validates JWT, flattens roles, provides ICurrentUser
		//   - Caches parsed claims in Redis (fast path on subsequent requests)
		//   - Enables token revocation blacklist (real-time logout)
		services.AddKeycloakRedisCache(configuration);
		#endregion

		#region 2. Swagger with Keycloak OAuth2 PKCE
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new() { Title = "Curvia API", Version = "v1" });
			c.AddKeycloakSecurityDefinition(services.BuildServiceProvider());
			c.AddKeycloakSecurityRequirement();
		});
		#endregion

		services.AddAuthentication();
		services.AddAuthorization();
	}
}

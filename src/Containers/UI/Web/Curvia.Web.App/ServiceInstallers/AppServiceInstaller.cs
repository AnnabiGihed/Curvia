using Radzen;
using System.Reflection.Metadata;
using Templates.Core.Tools.DependencyInjection;
using Templates.Core.Authentication.Blazor.Extensions;
using Templates.Core.Tools.DependencyInjection.Abstractions;

namespace Curvia.Web.App.ServiceInstallers;

public class AppServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
			base.IncludeConventionBasedRegistrations(services, configuration, [AssemblyReference.Assembly]);

		#region Radzen Services
		services.AddScoped<DialogService>();
		services.AddScoped<NotificationService>();
		services.AddScoped<TooltipService>();
		services.AddScoped<ContextMenuService>();
		services.AddScoped<ThemeService>();
		#endregion

		#region Redis (required by Keycloak Blazor session store)
		services.AddStackExchangeRedisCache(options =>
		{
			options.Configuration = configuration.GetConnectionString("Redis");
		});
		#endregion

		#region Authentication Services
		services.AddKeycloakBlazor(configuration);
		#endregion

		#region Typed HTTP client for Curvia API (bearer token auto-attached)
		services.AddHttpClient("CurviaApi", client =>
		{
			client.BaseAddress = new Uri(configuration["CurviaApi:BaseUrl"] ?? "https://localhost:7062");
		}).AddKeycloakHandler();
		#endregion
	}
}

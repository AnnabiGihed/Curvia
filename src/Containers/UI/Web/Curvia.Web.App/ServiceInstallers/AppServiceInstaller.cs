using Radzen;
using Templates.Core.Tools.DependencyInjection;
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

		//#region Authentication Services
		//services.AddKeycloakMaui(configuration);
		//services.AddAuthorizationCore();
		//services.AddScoped<AuthenticationStateProvider, KeycloakAuthStateProvider>();
		//#endregion

		//services.AddHttpClient("CurviaApi", client =>
		//{
		//	client.BaseAddress = new Uri("https://localhost:7062");
		//}).AddKeycloakHandler();
	}
}

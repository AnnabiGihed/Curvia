using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Tools.DependencyInjection;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Templates.Core.Tools.DependencyInjection.Abstractions;
using Curvia.Infrastructure.Features.Routing.Routes.Services;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla;

namespace Curvia.Infrastructure.ServiceInstallers;

public sealed class InfrastructureServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
		{
			base.IncludeConventionBasedRegistrations(services, configuration, new List<Assembly>() { AssemblyReference.Assembly }.ToArray());
		}

		#region Valhalla

		services.Configure<ValhallaOptions>(configuration.GetSection("Routing:Valhalla"));

		services.AddHttpClient<IValhallaRoutingClient, ValhallaRoutingClient>();

		#endregion

		#region Routing services
		services.AddScoped<IRouteBuilder, RouteBuilder>();
		services.AddScoped<IRouteScoringService, RouteScoringService>();
		#endregion

	}
}
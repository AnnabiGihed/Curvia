using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Tools.DependencyInjection;
using Curvia.Application.Features.Routing.Routes.Contracts;
using Templates.Core.Tools.DependencyInjection.Abstractions;
using Curvia.Infrastructure.Features.Routing.Routes.Services;
using Curvia.Infrastructure.Features.Routing.Routes.Engines.Valhalla;

namespace Curvia.Infrastructure.ServiceInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Registers all Infrastructure-layer services.
///              Note: convention-based registration (Scrutor) is globally disabled by the host
///              (HostingExtensions passes includeConventionBasedRegistration = false).
///              All services are registered explicitly here.
/// </summary>
public sealed class InfrastructureServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		#region Valhalla

		services.Configure<ValhallaOptions>(configuration.GetSection("Routing:Valhalla"));

		// Typed HttpClient — must be registered before the Scrutor scan would find ValhallaRoutingClient,
		// so the HttpClient factory is used (not a simple scoped registration).
		services.AddHttpClient<IValhallaRoutingClient, ValhallaRoutingClient>();

		#endregion

		#region Routing services

		services.AddScoped<IRouteCandidateGenerator, RouteCandidateGenerator>();
		services.AddScoped<IRouteBuilder, RouteBuilder>();
		services.AddScoped<IRouteScoringService, RouteScoringService>();

		#endregion
	}
}
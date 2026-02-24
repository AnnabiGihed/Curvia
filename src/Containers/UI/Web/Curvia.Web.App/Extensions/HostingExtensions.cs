using Curvia.App.Shared.Middlewares;
using System.Reflection;
using Templates.Core.Tools.DependencyInjection.Abstractions;

namespace Curvia.Web.App.Extensions;

public static class HostingExtensions
{
	public static WebApplication ConfigurePipeline(this WebApplication app)
	{
		app.UseMiddleware<ExceptionLoggingMiddleware>();
		app.UseMiddleware<LoggingMiddleware>();

		if (app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
			app.UseHsts();
		}

		app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
		app.UseHttpsRedirection();

		app.UseAntiforgery();

		app.MapStaticAssets();

		return app;
	}

	public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
	{
		builder.Services.InstallServices(builder.Configuration, false, AssemblyReference.Assembly);

		return builder.Build();
	}

	private static IServiceCollection InstallServices(this IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true, params Assembly[] assemblies)
	{
		IEnumerable<IServiceInstaller> serviceInstallers = assemblies
			.SelectMany(a => a.DefinedTypes)
			.Where(IsAssignableToType<IServiceInstaller>)
			.Select(Activator.CreateInstance)
			.Cast<IServiceInstaller>();

		foreach (IServiceInstaller serviceInstaller in serviceInstallers)
			serviceInstaller.Install(services, configuration, includeConventionBasedRegistration);

		return services;

		static bool IsAssignableToType<T>(TypeInfo typeInfo) => typeof(T).IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
	}
}

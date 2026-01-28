using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Serilog;
using System.Reflection;
using Templates.Core.Containers.API.Middleware;
using Templates.Core.Tools.DependencyInjection.Abstractions;

namespace Curvia.API;

internal static class HostingExtensions
{
	public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
	{
		builder.Services.InstallServices(builder.Configuration, false, AssemblyReference.Assembly, Application.AssemblyReference.Assembly, Infrastructure.AssemblyReference.Assembly, Persistence.EntityFrameworkCore.AssemblyReference.Assembly);

		builder.Logging.AddConsole();

		return builder.Build();
	}

	public static WebApplication ConfigurePipeline(this WebApplication app)
	{
		app.UseMiddleware<ExceptionHandlerMiddleware>();
		app.UseMiddleware<OutboxProcessingMiddleware<CurviaDbContext>>();
		app.UseMiddleware<TransactionMiddleware<CurviaDbContext>>();

		app.UseSerilogRequestLogging();

		app.UseHttpsRedirection();
		app.UseAuthentication();

		app.UseAuthorization();

		app.UseCors("Open");

		app.MapControllers();

		return app;
	}

	private static IServiceCollection InstallServices(this IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true, params Assembly[] assemblies)
	{
		IEnumerable<IServiceInstaller> serviceInstallers = assemblies
			.SelectMany(a => a.DefinedTypes)
			.Where(IsAssignableToType<IServiceInstaller>)
			.Select(Activator.CreateInstance)
			.Cast<IServiceInstaller>();

		foreach (IServiceInstaller serviceInstaller in serviceInstallers)
		{
			serviceInstaller.Install(services, configuration, includeConventionBasedRegistration);
		}

		return services;

		static bool IsAssignableToType<T>(TypeInfo typeInfo) => typeof(T).IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
	}
}

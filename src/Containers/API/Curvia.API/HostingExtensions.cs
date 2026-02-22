using Serilog;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Containers.API.Middleware;
using Templates.Core.Tools.DependencyInjection.Abstractions;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;

namespace Curvia.API;

internal static class HostingExtensions
{
	public static WebApplication ConfigurePipeline(this WebApplication app)
	{
		app.UseMiddleware<ExceptionHandlerMiddleware>();
		app.UseMiddleware<OutboxProcessingMiddleware<CurviaDbContext>>();
		app.UseMiddleware<TransactionMiddleware<CurviaDbContext>>();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Curvia API V1");
			});
		}

		app.UseSerilogRequestLogging();

		app.UseHttpsRedirection();
		app.UseAuthentication();

		app.UseAuthorization();

		app.UseCors("Open");

		app.MapControllers();

		return app;
	}

	public static async Task ResetApplicationDataBaseAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();

		try
		{
			var context = scope.ServiceProvider.GetService<CurviaDbContext>();

			if (context != null)
			{
				await context.Database.EnsureDeletedAsync();

				await context.Database.EnsureCreatedAsync();
				await context.Database.MigrateAsync();
			}

		}
		catch (Exception ex)
		{
		}
	}

	public static async Task MigrateApplicationDataBaseAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();

		try
		{
			var context = scope.ServiceProvider.GetService<CurviaDbContext>();

			if (context != null)
			{
				await context.Database.MigrateAsync();
			}

		}
		catch (Exception ex)
		{
			//TODO: add logging ...
		}
	}

	public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
	{
		builder.Services.InstallServices(builder.Configuration, false, AssemblyReference.Assembly, Application.AssemblyReference.Assembly, Infrastructure.AssemblyReference.Assembly, Persistence.EntityFrameworkCore.AssemblyReference.Assembly);

		builder.Logging.AddConsole();

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
		{
			serviceInstaller.Install(services, configuration, includeConventionBasedRegistration);
		}

		return services;

		static bool IsAssignableToType<T>(TypeInfo typeInfo) => typeof(T).IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
	}
}

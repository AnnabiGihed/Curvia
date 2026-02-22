using Serilog;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Authentication.Extensions;
using Templates.Core.Containers.API.Middleware;
using Templates.Core.Tools.DependencyInjection.Abstractions;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Seed;

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
				c.UseKeycloakOAuth(app.Services);
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

	/// <summary>
	/// Seeds the official motorcycle maker/model catalog on first startup.
	/// Idempotent — skips if the table already has rows.
	/// Call AFTER MigrateApplicationDataBaseAsync.
	/// </summary>
	public static async Task SeedMotorcycleCatalogAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<CurviaDbContext>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogDataSeeder>>();

		try
		{
			await CatalogDataSeeder.SeedAsync(db, logger);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Motorcycle catalog seeding failed.");
		}
	}
	public static async Task ResetApplicationDataBaseAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

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
			app.Logger.LogError(ex, "Database reset failed.");
		}
	}
	public static async Task MigrateApplicationDataBaseAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

		try
		{
			var context = scope.ServiceProvider.GetService<CurviaDbContext>();
			if (context != null)
				await context.Database.MigrateAsync();
		}
		catch (Exception ex)
		{
			app.Logger.LogError(ex, "Database migration failed.");
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
		IEnumerable<IServiceInstaller> installers = assemblies
			.SelectMany(a => a.DefinedTypes)
			.Where(IsAssignableToType<IServiceInstaller>)
			.Select(Activator.CreateInstance)
			.Cast<IServiceInstaller>();

		foreach (IServiceInstaller installer in installers)
			installer.Install(services, configuration, includeConventionBasedRegistration);

		return services;

		static bool IsAssignableToType<T>(TypeInfo typeInfo) =>	typeof(T).IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
	}
}
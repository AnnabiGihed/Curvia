using Curvia.API.Middleware.Identity;
using Curvia.Infrastructure.Jobs;
using Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Seed;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Authentication.AspNetCore.Extensions;
using Pivot.Framework.Authentication.Hangfire.Extensions;
using Pivot.Framework.Containers.API.Middleware;
using Pivot.Framework.Infrastructure.Scheduling.Extensions;
using Pivot.Framework.Tools.DependencyInjection.Abstractions;
using Serilog;
using System.Reflection;

namespace Curvia.API;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Provides WebApplication/WebApplicationBuilder extension methods to wire up:
///              - The HTTP middleware pipeline (exception handling, outbox, transactions, auth, routing)
///              - Swagger + Keycloak OAuth in development
///              - JIT user provisioning middleware
///              - Hangfire dashboard (via Pivot.Framework.Infrastructure.Scheduling)
///              - Awareness recurring-job registration
///              - Database reset/migration helpers
///              - Dependency-injection service installer discovery across assemblies
/// </summary>
internal static class HostingExtensions
{
	#region Public Methods
	/// <summary>
	/// Configures the ASP.NET Core request pipeline (middleware order, dev tooling, auth, routing, CORS).
	/// </summary>
	/// <param name="app">Web application instance.</param>
	/// <returns>The same <see cref="WebApplication"/> instance for fluent chaining.</returns>
	public static WebApplication ConfigurePipeline(this WebApplication app)
	{
		#region Infrastructure middleware
		app.UseMiddleware<ExceptionHandlerMiddleware>();
		app.UseMiddleware<OutboxProcessingMiddleware<CurviaDbContext>>();
		app.UseMiddleware<TransactionMiddleware<CurviaDbContext>>();
		#endregion

		#region Dev tooling
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Curvia API V1");
				c.UseKeycloakOAuth(app.Services);
			});
		}
		#endregion

		#region Standard ASP.NET Core pipeline
		app.UseSerilogRequestLogging();
		app.UseHttpsRedirection();
		app.UseAuthentication();
		app.UseAuthorization();
		#endregion

		#region JIT User Provisioning
		app.UseMiddleware<JitProvisioningMiddleware>();
		#endregion

		#region Routing & CORS
		app.UseCors("Open");
		app.MapControllers();
		#endregion

		#region Hangfire Dashboard
		// Mounted at /hangfire — restrict to dev/internal networks in production via DashboardOptions.
		app.UseHangfireDashboardWithKeycloakAuth(opts =>
		{
			opts.DashboardTitle = "Curvia — Background Jobs";
		});
		#endregion

		#region Awareness recurring jobs
		// AwarenessJobsSetup lives in Curvia.Infrastructure.Jobs and registers all
		// speed-camera, hazard, road-work, incident, and cleanup recurring jobs with Hangfire.
		// Must be called AFTER UseHangfireDashboard so the Hangfire server is fully initialised.
		app.RegisterAwarenessJobs();
		#endregion

		return app;
	}

	/// <summary>
	/// Seeds the official motorcycle maker/model catalog on first startup.
	/// Idempotent — skips if the table already has rows.
	/// Call AFTER MigrateApplicationDataBaseAsync.
	/// </summary>
	/// <param name="app">Web application instance.</param>
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

	/// <summary>
	/// Drops and recreates the application database, then applies migrations.
	/// Intended for local/dev scenarios.
	/// </summary>
	/// <param name="app">Web application instance.</param>
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

	/// <summary>
	/// Applies pending EF Core migrations for the application database.
	/// </summary>
	/// <param name="app">Web application instance.</param>
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
			logger.LogError(ex, "Database migration failed.");
		}
	}

	/// <summary>
	/// Configures the DI container by discovering <see cref="IServiceInstaller"/> implementations
	/// from the provided assemblies, then builds the <see cref="WebApplication"/>.
	/// </summary>
	/// <param name="builder">Web application builder.</param>
	/// <returns>The built <see cref="WebApplication"/>.</returns>
	public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
	{
		builder.Services.InstallServices(builder.Configuration, false, AssemblyReference.Assembly, Application.AssemblyReference.Assembly, Infrastructure.AssemblyReference.Assembly, Persistence.EntityFrameworkCore.AssemblyReference.Assembly);

		builder.Logging.AddConsole();

		return builder.Build();
	}
	#endregion

	#region Private Methods
	/// <summary>
	/// Discovers and executes <see cref="IServiceInstaller"/> implementations found in the given assemblies.
	/// </summary>
	/// <param name="services">The DI service collection.</param>
	/// <param name="configuration">Application configuration.</param>
	/// <param name="includeConventionBasedRegistration">When true, installers may also include convention-based registrations.</param>
	/// <param name="assemblies">Assemblies to scan for installers.</param>
	/// <returns>The updated <see cref="IServiceCollection"/>.</returns>
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

		/// <summary>
		/// Determines whether a <see cref="TypeInfo"/> can be assigned to <typeparamref name="T"/>
		/// and is concrete (non-interface, non-abstract).
		/// </summary>
		/// <typeparam name="T">Target type.</typeparam>
		/// <param name="typeInfo">Type information to test.</param>
		/// <returns>True when assignable and concrete; otherwise false.</returns>
		static bool IsAssignableToType<T>(TypeInfo typeInfo) => typeof(T).IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract;
	}
	#endregion
}
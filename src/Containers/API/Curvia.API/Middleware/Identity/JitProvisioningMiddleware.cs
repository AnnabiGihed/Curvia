using Microsoft.EntityFrameworkCore;
using Pivot.Framework.Domain.Primitives;
using Pivot.Framework.Authentication.Models;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Domain.Features.Users.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;

namespace Curvia.API.Middleware.Identity;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Guarantees that a User row exists in AppUsers for every authenticated request.
/// </summary>
public sealed class JitProvisioningMiddleware
{
	#region Fields
	private readonly RequestDelegate _next;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<JitProvisioningMiddleware> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initializes the JIT provisioning middleware.
	/// </summary>
	/// <param name="next">Next middleware in the pipeline.</param>
	/// <param name="scopeFactory">Scope factory used to create an isolated scope for provisioning and persistence.</param>
	/// <param name="logger">Logger instance.</param>
	public JitProvisioningMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, ILogger<JitProvisioningMiddleware> logger)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
	}
	#endregion

	#region Private helpers
	/// <summary>
	/// Builds a new <see cref="User"/> from the JWT claims.
	/// Applies safe fallbacks for every claim so provisioning never fails on
	/// a partially populated token.
	/// </summary>
	/// <param name="currentUser">Current authenticated user abstraction.</param>
	/// <param name="keycloakGuid">Keycloak subject GUID.</param>
	/// <returns>A newly created <see cref="User"/> aggregate.</returns>
	private static User CreateUser(ICurrentUser currentUser, Guid keycloakGuid)
	{
		var keycloakId = keycloakGuid.ToString();
		var locale = currentUser.Principal?.FindFirst("locale")?.Value;
		var email = currentUser.Email ?? $"{keycloakId}@unknown.invalid";
		var displayName = currentUser.DisplayName ?? currentUser.Username ?? keycloakId;

		var result = User.Provision(keycloakId, email, displayName, locale);

		if (result.IsFailure)
			throw new InvalidOperationException($"JIT User.Create() failed for Keycloak sub {keycloakId}: {result.Error.Message}");

		return result.Value;
	}

	/// <summary>
	/// Resolves or creates the User record. Returns the application-level User.Id.
	/// Runs in a dedicated DI scope so its SaveChangesAsync is independent.
	/// </summary>
	/// <param name="currentUser">Current authenticated user abstraction.</param>
	/// <param name="keycloakGuid">Keycloak subject GUID.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Application-level User identifier (Guid).</returns>
	private async Task<Guid> ProvisionUserAsync(ICurrentUser currentUser, Guid keycloakGuid, CancellationToken ct)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();

		var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
		var dbContext = scope.ServiceProvider.GetRequiredService<CurviaDbContext>();

		var keycloakIdResult = KeycloakSubject.Create(keycloakGuid.ToString());
		if (keycloakIdResult.IsFailure)
			throw new InvalidOperationException($"Failed to build KeycloakId from sub {keycloakGuid}: {keycloakIdResult.Error.Message}");

		var keycloakId = keycloakIdResult.Value;
		var user = await userRepo.FindByKeycloakIdAsync(keycloakId, ct);

		if (user is null)
		{
			user = CreateUser(currentUser, keycloakGuid);
			await userRepo.AddAsync(user, ct);

			_logger.LogInformation("JIT provisioned new User {UserId} for Keycloak sub {KeycloakSub}.", user.Id.Value, keycloakGuid);
		}
		else
		{
			var email = currentUser.Email ?? user.Email.Value;
			var locale = currentUser.Principal?.FindFirst("locale")?.Value;
			var displayName = currentUser.DisplayName ?? user.DisplayName.Value;

			var updateResult = user.SyncFromToken(email, displayName, locale);

			if (updateResult.IsFailure)
			{
				_logger.LogWarning("JIT profile sync failed for User {UserId}: {Error}", user.Id.Value, updateResult.Error.Message);
			}
		}

		var now = DateTime.UtcNow;

		foreach (var entry in dbContext.ChangeTracker.Entries<IAuditableEntity>())
		{
			if (entry.State == EntityState.Added)
				entry.Entity.SetAudit(AuditInfo.Create(now, "system:jit"));
			else if (entry.State == EntityState.Modified)
				entry.Entity.Audit.Modify(now, "system:jit");
		}

		await dbContext.SaveChangesAsync(ct);

		return user.Id.Value;
	}
	#endregion

	#region Middleware entry point
	/// <summary>
	/// Middleware entry point. For authenticated requests, ensures an AppUsers row exists
	/// (creating it if needed) and stores the application UserId in <see cref="IAppUserContext"/>.
	/// </summary>
	/// <param name="context">Current HTTP context.</param>
	public async Task InvokeAsync(HttpContext context)
	{
		var currentUser = context.RequestServices.GetRequiredService<ICurrentUser>();

		if (!currentUser.IsAuthenticated || currentUser.UserId is not { } keycloakGuid)
		{
			await _next(context);
			return;
		}

		Guid appUserId;

		try
		{
			appUserId = await ProvisionUserAsync(currentUser, keycloakGuid, context.RequestAborted);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "JIT provisioning failed for Keycloak sub {KeycloakSub}. Request will be aborted.", keycloakGuid);
			throw;
		}

		var appUserContext = context.RequestServices.GetRequiredService<IAppUserContext>();
		appUserContext.SetAppUserId(appUserId);

		await _next(context);
	}
	#endregion
}
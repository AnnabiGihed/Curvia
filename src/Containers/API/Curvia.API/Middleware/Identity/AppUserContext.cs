namespace Curvia.API.Middleware.Identity;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Scoped implementation of <see cref="IAppUserContext"/>.
///              Starts null. JitProvisioningMiddleware calls SetAppUserId() once per request.
///              Controllers and downstream services read AppUserId after that.
/// </summary>
public sealed class AppUserContext : IAppUserContext
{
	#region Properties
	/// <summary>
	/// Gets the application-level user identifier resolved during the current request.
	/// Null until set by <see cref="JitProvisioningMiddleware"/>.
	/// </summary>
	public Guid? AppUserId { get; private set; }
	#endregion

	#region Public Methods
	/// <summary>
	/// Sets the application-level user identifier for the current request scope.
	/// Intended to be called once by the JIT provisioning middleware.
	/// </summary>
	/// <param name="userId">Application user identifier.</param>
	public void SetAppUserId(Guid userId) => AppUserId = userId;
	#endregion
}
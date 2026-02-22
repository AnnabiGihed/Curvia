namespace Curvia.API.Middleware.Identity;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Holds the application-level User.Id for the current HTTP request.
/// </summary>
public interface IAppUserContext
{
	/// <summary>
	/// The application-level User.Id resolved by JitProvisioningMiddleware.
	/// Null when the request is unauthenticated.
	/// </summary>
	Guid? AppUserId { get; }

	/// <summary>
	/// Called exclusively by JitProvisioningMiddleware after the user is resolved or created.
	/// </summary>
	void SetAppUserId(Guid userId);
}
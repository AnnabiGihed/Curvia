using Microsoft.AspNetCore.Components;
using Templates.Core.Authentication.Blazor.Services;

namespace Curvia.Web.App.Components.Layout;

public partial class MainLayout
{
	#region Dependencies
	[Inject]
	IBlazorKeycloakAuthService Auth { get; set; } = null!;
	#endregion

	#region Lifecycle
	protected override async Task OnInitializedAsync()
	{
		// Rehydrate the Keycloak session from the HttpOnly session cookie on every
		// new Blazor circuit (page load / reconnect).
		await Auth.InitialiseFromCookieAsync();
	}
	#endregion
}

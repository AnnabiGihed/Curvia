using Curvia.Web.App.Components;
using Curvia.Web.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

var app = builder
	.ConfigureServices()
	.ConfigurePipeline();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

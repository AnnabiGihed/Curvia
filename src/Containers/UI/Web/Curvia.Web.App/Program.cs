using Curvia.Web.App.Components;
using Curvia.Web.App.Extensions;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

var app = builder
	.ConfigureServices()
	.ConfigurePipeline();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

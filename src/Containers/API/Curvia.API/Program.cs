using Serilog;
using Curvia.API;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.WriteTo.Console().ReadFrom.Configuration(context.Configuration));

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	serverOptions.Limits.MaxRequestBodySize = 524288000;
});

var app = builder
	.ConfigureServices()
	.ConfigurePipeline();

await app.MigrateApplicationDataBaseAsync();

app.Run();
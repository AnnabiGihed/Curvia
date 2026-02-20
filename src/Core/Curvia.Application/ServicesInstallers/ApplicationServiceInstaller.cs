using MediatR;
using FluentValidation;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Templates.Core.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Templates.Core.Tools.DependencyInjection;
using Templates.Core.Tools.DependencyInjection.Abstractions;


namespace Curvia.Application.ServicesInstallers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Registers all Application-layer services:
///              - MediatR (command/query dispatching)
///              - FluentValidation validators (auto-discovered from this assembly)
///              - ValidationPipelineBehavior (pre-handler validation via MediatR pipeline)
/// </summary>
public sealed class ApplicationServiceInstaller : BaseServiceInstaller, IServiceInstaller
{
	public void Install(IServiceCollection services, IConfiguration configuration, bool includeConventionBasedRegistration = true)
	{
		if (includeConventionBasedRegistration)
			base.IncludeConventionBasedRegistrations(services, configuration, new List<Assembly>() { AssemblyReference.Assembly }.ToArray());

		#region MediatR
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly));
		#endregion

		#region FluentValidation
		services.AddValidatorsFromAssembly(AssemblyReference.Assembly, includeInternalTypes: true);
		#endregion

		#region Validation Pipeline Behavior
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
		#endregion
	}
}
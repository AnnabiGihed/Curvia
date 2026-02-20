using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.Routing.Routes.Services;

public interface IValhallaSmokeTestService
{
	Task<Result<ValhallaSmokeTestResult>> RunAsync(CancellationToken cancellationToken = default);
}
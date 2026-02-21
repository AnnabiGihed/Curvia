using Templates.Core.Domain.Shared;

namespace Curvia.Application.Features.Routes.Services;

public interface IValhallaSmokeTestService
{
	Task<Result<ValhallaSmokeTestResult>> RunAsync(CancellationToken cancellationToken = default);
}
using Templates.Core.Domain.Shared;
using Curvia.Application.Features.Routing.Routes.Services;
using Templates.Core.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.Routing.Routes.Queries.SmokeTest;

internal sealed class ValhallaSmokeTestQueryHandler : IQueryHandler<ValhallaSmokeTestQuery, ValhallaSmokeTestResponse>
{
	#region Properties
	private readonly IValhallaSmokeTestService _smoke;
	#endregion

	#region Constructors
	public ValhallaSmokeTestQueryHandler(IValhallaSmokeTestService smoke)
	{
		_smoke = smoke ?? throw new ArgumentNullException(nameof(smoke));
	}
	#endregion

	#region IQueryHandler Implementation
	public async Task<Result<ValhallaSmokeTestResponse>> Handle(ValhallaSmokeTestQuery request, CancellationToken cancellationToken)
	{
		var res = await _smoke.RunAsync(cancellationToken);

		if (res.IsFailure)
			return Result.Failure<ValhallaSmokeTestResponse>(res.Error, res.ResultExceptionType);

		var value = res.Value;

		return Result.Success(new ValhallaSmokeTestResponse(value.DistanceKm, value.TimeSeconds, value.LegsCount, value.HasShape, value.StatusMessage));
	}
	#endregion
}
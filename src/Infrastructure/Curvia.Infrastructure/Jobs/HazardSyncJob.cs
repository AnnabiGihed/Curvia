using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using Curvia.Application.Features.Awareness.Commands.SyncHazards;

namespace Curvia.Infrastructure.Jobs;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Hangfire recurring job that triggers the hazard sync pipeline
///              for a single country by dispatching a SyncHazardsCommand via MediatR.
///              Logs job lifecycle and all failures to Serilog so errors are visible in Kibana.
/// </summary>
public sealed class HazardSyncJob
{
	#region Fields
	/// <summary>MediatR sender used to dispatch the sync command.</summary>
	private readonly IMediator _mediator;

	/// <summary>Logger for job lifecycle and error events.</summary>
	private readonly ILogger<HazardSyncJob> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="HazardSyncJob"/>.
	/// </summary>
	/// <param name="mediator">MediatR sender.</param>
	/// <param name="logger">Logger instance.</param>
	public HazardSyncJob(IMediator mediator, ILogger<HazardSyncJob> logger)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	#endregion

	#region Job
	/// <summary>
	/// Executes the hazard sync for the given country.
	/// Throws on failure so Hangfire marks the job as failed.
	/// </summary>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code to sync.</param>
	/// <param name="ct">Cancellation token.</param>
	[AutomaticRetry(Attempts = 0)]
	public async Task RunAsync(string countryCode, CancellationToken ct)
	{
		_logger.LogInformation("[SyncJob] Hazard sync starting for {CountryCode}.", countryCode);

		try
		{
			var result = await _mediator.Send(new SyncHazardsCommand(countryCode), ct);

			if (result.IsFailure)
			{
				_logger.LogError("[SyncJob] Hazard sync failed for {CountryCode}. Reason: {Error}", countryCode, result.Error);
				throw new InvalidOperationException($"Hazard sync failed for {countryCode}: {result.Error}");
			}

			_logger.LogInformation("[SyncJob] Hazard sync completed successfully for {CountryCode}.", countryCode);
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			_logger.LogError(ex, "[SyncJob] Hazard sync threw an unhandled exception for {CountryCode}.", countryCode);
			throw;
		}
	}
	#endregion
}
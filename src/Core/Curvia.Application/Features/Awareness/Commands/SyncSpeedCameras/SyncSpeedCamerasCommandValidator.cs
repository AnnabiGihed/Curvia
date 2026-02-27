using Microsoft.Extensions.Logging;
using Curvia.Domain.Features.Awareness.Repositories;

namespace Curvia.Infrastructure.Jobs;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Standalone Hangfire job that purges expired RoadWork and Incident rows.
///              Separated from the sync commands because it operates across all countries
///              in a single query — dispatching one MediatR command per country would
///              be unnecessary overhead for a simple delete.
/// </summary>
public sealed class AwarenessCleanupJob
{
	#region Dependencies
	/// <summary>Persistence contract for <see cref="RoadWork"/> aggregates.</summary>
	private readonly IRoadWorkRepository _roadWorkRepo;

	/// <summary>Persistence contract for <see cref="Incident"/> aggregates.</summary>
	private readonly IIncidentRepository _incidentRepo;

	/// <summary>Logger for this cleanup job.</summary>
	private readonly ILogger<AwarenessCleanupJob> _logger;
	#endregion

	#region Constructor
	/// <summary>
	/// Initialises a new instance of <see cref="AwarenessCleanupJob"/>.
	/// </summary>
	/// <param name="roadWorkRepo">Persistence contract for road-work aggregates.</param>
	/// <param name="incidentRepo">Persistence contract for incident aggregates.</param>
	/// <param name="logger">Logger instance.</param>
	public AwarenessCleanupJob(IRoadWorkRepository roadWorkRepo, IIncidentRepository incidentRepo, ILogger<AwarenessCleanupJob> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_roadWorkRepo = roadWorkRepo ?? throw new ArgumentNullException(nameof(roadWorkRepo));
		_incidentRepo = incidentRepo ?? throw new ArgumentNullException(nameof(incidentRepo));
	}
	#endregion

	#region Job
	/// <summary>
	/// Deletes all expired road-work and incident rows in a single pass.
	/// Called hourly by Hangfire to keep awareness tables lean.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	public async Task RunAsync(CancellationToken ct)
	{
		_logger.LogInformation("Awareness cleanup: purging expired road works and incidents.");
		await _roadWorkRepo.DeleteExpiredAsync(ct);
		await _incidentRepo.DeleteExpiredAsync(ct);
		_logger.LogInformation("Awareness cleanup: complete.");
	}
	#endregion
}
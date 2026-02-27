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
	private readonly IRoadWorkRepository _roadWorkRepo;
	private readonly IIncidentRepository _incidentRepo;
	private readonly ILogger<AwarenessCleanupJob> _logger;

	public AwarenessCleanupJob(IRoadWorkRepository roadWorkRepo, IIncidentRepository incidentRepo, ILogger<AwarenessCleanupJob> logger)
	{
		_logger = logger;
		_roadWorkRepo = roadWorkRepo;
		_incidentRepo = incidentRepo;

	}

	public async Task RunAsync(CancellationToken ct)
	{
		_logger.LogInformation("Awareness cleanup: purging expired road works and incidents.");
		await _roadWorkRepo.DeleteExpiredAsync(ct);
		await _incidentRepo.DeleteExpiredAsync(ct);
		_logger.LogInformation("Awareness cleanup: complete.");
	}
}
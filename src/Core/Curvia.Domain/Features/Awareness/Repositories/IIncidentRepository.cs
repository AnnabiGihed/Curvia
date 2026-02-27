using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Templates.Core.Domain.Repositories;

namespace Curvia.Domain.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Write + read contract for <see cref="Incident"/> persistence.
///              GetInAreaAsync automatically filters to currently-active incidents.
/// </summary>
public interface IIncidentRepository : IAsyncCommandRepository<Incident, IncidentId>
{
	Task<IReadOnlyList<Incident>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default);
	Task<Incident?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default);
	Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default);

	/// <summary>
	/// Deletes all incidents where ValidUntilUtc &lt; UtcNow.
	/// Called after each DATEX II sync run.
	/// </summary>
	Task DeleteExpiredAsync(CancellationToken ct = default);
}
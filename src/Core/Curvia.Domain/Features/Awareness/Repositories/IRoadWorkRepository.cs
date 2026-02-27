using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Templates.Core.Domain.Repositories;

namespace Curvia.Domain.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Write + read contract for <see cref="RoadWork"/> persistence.
///              GetInAreaAsync automatically filters to currently-active records
///              (ValidFromUtc ≤ UtcNow &lt; ValidUntilUtc) — expiry is a query concern,
///              not a domain invariant enforced by the aggregate.
/// </summary>
public interface IRoadWorkRepository : IAsyncCommandRepository<RoadWork, RoadWorkId>
{
	Task<IReadOnlyList<RoadWork>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default);
	Task<RoadWork?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default);
	Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default);

	/// <summary>
	/// Deletes all road works where ValidUntilUtc &lt; UtcNow.
	/// Called periodically by the Hangfire cleanup job.
	/// </summary>
	Task DeleteExpiredAsync(CancellationToken ct = default);
}
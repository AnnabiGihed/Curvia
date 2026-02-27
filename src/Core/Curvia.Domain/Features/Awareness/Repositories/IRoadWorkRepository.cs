using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;

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
	/// <summary>
	/// Returns all active road works whose coordinates fall within the bounding box.
	/// Active means ValidFromUtc ≤ UtcNow &lt; ValidUntilUtc — filtering is applied server-side.
	/// </summary>
	/// <param name="bbox">Geographic bounding box defining the query area.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Read-only list of active <see cref="RoadWork"/> aggregates within the area.</returns>
	Task<IReadOnlyList<RoadWork>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default);

	/// <summary>
	/// Finds an existing road work by its natural key (ExternalId + Source + CountryCode).
	/// Returns null when not found — used by the Worker to decide create vs update.
	/// </summary>
	/// <param name="externalId">External identifier from the source dataset.</param>
	/// <param name="source">Short source name (e.g. "datex2").</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g. "BE").</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The matching <see cref="RoadWork"/>, or null when not found.</returns>
	Task<RoadWork?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default);

	/// <summary>
	/// Removes all road works for a given source + country in a single bulk statement.
	/// Called by the Worker before reloading a full country dataset.
	/// </summary>
	/// <param name="source">Short source name identifying the dataset origin.</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code scoping the deletion.</param>
	/// <param name="ct">Cancellation token.</param>
	Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default);

	/// <summary>
	/// Deletes all road works where ValidUntilUtc &lt; UtcNow.
	/// Called periodically by the Hangfire cleanup job.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	Task DeleteExpiredAsync(CancellationToken ct = default);
}
using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;

namespace Curvia.Domain.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Write + read contract for <see cref="Hazard"/> persistence.
///              Hazard is a trivial single-entity aggregate root — own consistency boundary,
///              accessed directly by a repository, no child entities.
/// </summary>
public interface IHazardRepository : IAsyncCommandRepository<Hazard, HazardId>
{
	/// <summary>
	/// Returns all hazards whose coordinates fall within the bounding box.
	/// </summary>
	/// <param name="bbox">Geographic bounding box defining the query area.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Read-only list of <see cref="Hazard"/> aggregates within the area.</returns>
	Task<IReadOnlyList<Hazard>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default);

	/// <summary>
	/// Finds an existing hazard by its natural key (ExternalId + Source + CountryCode).
	/// Returns null when not found — used by the Worker to decide create vs update.
	/// </summary>
	/// <param name="externalId">External identifier from the source dataset.</param>
	/// <param name="source">Short source name (e.g. "osm").</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g. "BE").</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The matching <see cref="Hazard"/>, or null when not found.</returns>
	Task<Hazard?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default);

	/// <summary>
	/// Removes all hazards for a given source + country in a single bulk statement.
	/// Called by the Worker before reloading a full country dataset.
	/// </summary>
	/// <param name="source">Short source name identifying the dataset origin.</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code scoping the deletion.</param>
	/// <param name="ct">Cancellation token.</param>
	Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default);
}
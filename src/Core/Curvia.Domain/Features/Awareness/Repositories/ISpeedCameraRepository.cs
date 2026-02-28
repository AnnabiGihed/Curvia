using Curvia.Domain.Shared.ValueObjects;
using Pivot.Framework.Domain.Repositories;
using Curvia.Domain.Features.Awareness.Aggregates;

namespace Curvia.Domain.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Write + read contract for <see cref="SpeedCamera"/> persistence.
///              SpeedCamera is a trivial single-entity aggregate root — it has no child
///              entities but forms its own consistency boundary and is accessed directly
///              by a repository, which is the defining characteristic of an aggregate root.
///              Domain events are optional machinery and are not required here.
///              Inherits IAsyncCommandRepository for standard Add/Update/Delete operations.
///              Additional methods cover the Worker's bulk sync operations and the API's
///              spatial read queries.
/// </summary>
public interface ISpeedCameraRepository : IAsyncCommandRepository<SpeedCamera, SpeedCameraId>
{
	/// <summary>
	/// Returns all speed cameras whose coordinates fall within the bounding box.
	/// </summary>
	/// <param name="bbox">Geographic bounding box defining the query area.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Read-only list of <see cref="SpeedCamera"/> aggregates within the area.</returns>
	Task<IReadOnlyList<SpeedCamera>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default);

	/// <summary>
	/// Finds an existing camera by its natural key (ExternalId + Source + CountryCode).
	/// Returns null when not found — used by the Worker to decide create vs update.
	/// </summary>
	/// <param name="externalId">External identifier from the source dataset.</param>
	/// <param name="source">Short source name (e.g. "osm", "openspeedcam").</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g. "BE").</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The matching <see cref="SpeedCamera"/>, or null when not found.</returns>
	Task<SpeedCamera?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default);

	/// <summary>
	/// Removes all cameras for a given source + country in a single bulk statement.
	/// Called by the Worker before reloading a full country dataset.
	/// </summary>
	/// <param name="source">Short source name identifying the dataset origin.</param>
	/// <param name="countryCode">ISO 3166-1 alpha-2 country code scoping the deletion.</param>
	/// <param name="ct">Cancellation token.</param>
	Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default);
}
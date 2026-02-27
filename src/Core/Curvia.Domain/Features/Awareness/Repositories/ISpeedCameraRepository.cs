using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.ValueObjects;

namespace Curvia.Domain.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Write + read contract for <see cref="SpeedCamera"/> persistence.
///
///              SpeedCamera is a trivial single-entity aggregate root — it has no child
///              entities but forms its own consistency boundary and is accessed directly
///              by a repository, which is the defining characteristic of an aggregate root.
///              Domain events are optional machinery and are not required here.
///
///              Inherits IAsyncCommandRepository for standard Add/Update/Delete operations.
///              Additional methods cover the Worker's bulk sync operations and the API's
///              spatial read queries.
/// </summary>
public interface ISpeedCameraRepository : IAsyncCommandRepository<SpeedCamera, SpeedCameraId>
{
	/// <summary>Returns all speed cameras whose coordinates fall within the bounding box.</summary>
	Task<IReadOnlyList<SpeedCamera>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default);

	/// <summary>
	/// Finds an existing camera by its natural key (ExternalId + Source + CountryCode).
	/// Returns null when not found — used by the Worker to decide create vs update.
	/// </summary>
	Task<SpeedCamera?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default);

	/// <summary>
	/// Removes all cameras for a given source + country in a single bulk statement.
	/// Called by the Worker before reloading a full country dataset.
	/// </summary>
	Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default);
}
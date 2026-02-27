using Templates.Core.Domain.Repositories;
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
	Task<IReadOnlyList<Hazard>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default);
	Task<Hazard?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default);
	Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default);
}
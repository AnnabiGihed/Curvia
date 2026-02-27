using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IHazardRepository"/>.
///              Hazard is a trivial aggregate root — single-entity aggregate.
/// </summary>
internal sealed class HazardRepository
	: BaseAsyncCommandRepository<Hazard, HazardId>, IHazardRepository
{
	public HazardRepository(CurviaDbContext dbContext) : base(dbContext) { }

	public async Task<IReadOnlyList<Hazard>> GetInAreaAsync(
		BoundingBox bbox, CancellationToken ct = default)
	{
		return await DbContext.Set<Hazard>()
			.Where(h => h.Latitude >= bbox.South && h.Latitude <= bbox.North
					 && h.Longitude >= bbox.West && h.Longitude <= bbox.East)
			.ToListAsync(ct);
	}

	public async Task<Hazard?> FindByExternalIdAsync(
		string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		return await DbContext.Set<Hazard>()
			.FirstOrDefaultAsync(h =>
				h.ExternalId == externalId &&
				h.Source == source &&
				h.CountryCode == countryCode, ct);
	}

	public async Task DeleteBySourceAsync(
		string source, string countryCode, CancellationToken ct = default)
	{
		await DbContext.Set<Hazard>()
			.Where(h => h.Source == source && h.CountryCode == countryCode)
			.ExecuteDeleteAsync(ct);
	}
}

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
/// Purpose     : EF Core implementation of <see cref="IRoadWorkRepository"/>.
///              GetInAreaAsync filters to active records server-side.
/// </summary>
internal sealed class RoadWorkRepository
	: BaseAsyncCommandRepository<RoadWork, RoadWorkId>, IRoadWorkRepository
{
	public RoadWorkRepository(CurviaDbContext dbContext) : base(dbContext) { }

	public async Task<IReadOnlyList<RoadWork>> GetInAreaAsync(
		BoundingBox bbox, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await DbContext.Set<RoadWork>()
			.Where(r => r.Latitude >= bbox.South && r.Latitude <= bbox.North
					 && r.Longitude >= bbox.West && r.Longitude <= bbox.East
					 && r.ValidFromUtc <= now
					 && r.ValidUntilUtc > now)
			.ToListAsync(ct);
	}

	public async Task<RoadWork?> FindByExternalIdAsync(
		string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		return await DbContext.Set<RoadWork>()
			.FirstOrDefaultAsync(r =>
				r.ExternalId == externalId &&
				r.Source == source &&
				r.CountryCode == countryCode, ct);
	}

	public async Task DeleteBySourceAsync(
		string source, string countryCode, CancellationToken ct = default)
	{
		await DbContext.Set<RoadWork>()
			.Where(r => r.Source == source && r.CountryCode == countryCode)
			.ExecuteDeleteAsync(ct);
	}

	public async Task DeleteExpiredAsync(CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		await DbContext.Set<RoadWork>()
			.Where(r => r.ValidUntilUtc < now)
			.ExecuteDeleteAsync(ct);
	}
}
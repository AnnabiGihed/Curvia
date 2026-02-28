using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Shared.ValueObjects;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IHazardRepository"/>.
///              Hazard is a trivial aggregate root — single-entity aggregate, no child entities,
///              no soft-delete. Lifecycle is managed by the Worker's DeleteBySourceAsync + reload pattern.
/// </summary>
internal sealed class HazardRepository : BaseAsyncCommandRepository<Hazard, HazardId>, IHazardRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="HazardRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public HazardRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region IHazardRepository
	/// <inheritdoc/>
	public async Task<IReadOnlyList<Hazard>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default)
	{
		return await DbContext.Set<Hazard>()
			.Where(h => h.Position.Latitude >= bbox.South && h.Position.Latitude <= bbox.North
					 && h.Position.Longitude >= bbox.West && h.Position.Longitude <= bbox.East)
			.ToListAsync(ct);
	}

	/// <inheritdoc/>
	public async Task<Hazard?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		return await DbContext.Set<Hazard>()
			.FirstOrDefaultAsync(h =>
				h.ExternalId.Value == externalId &&
				h.Source.Value == source &&
				h.CountryCode.Value == countryCode, ct);
	}

	/// <inheritdoc/>
	public async Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default)
	{
		await DbContext.Set<Hazard>()
			.Where(h => h.Source.Value == source && h.CountryCode.Value == countryCode)
			.ExecuteDeleteAsync(ct);
	}
	#endregion
}
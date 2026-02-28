using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IIncidentRepository"/>.
///              GetInAreaAsync filters to currently-active incidents server-side
///              (ValidFromUtc ≤ UtcNow &lt; ValidUntilUtc).
/// </summary>
internal sealed class IncidentRepository : BaseAsyncCommandRepository<Incident, IncidentId>, IIncidentRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="IncidentRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public IncidentRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region IIncidentRepository
	/// <inheritdoc/>
	public async Task<IReadOnlyList<Incident>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await DbContext.Set<Incident>()
			.Where(i => i.Position.Latitude >= bbox.South && i.Position.Latitude <= bbox.North
					 && i.Position.Longitude >= bbox.West && i.Position.Longitude <= bbox.East
					 && i.ValidFromUtc <= now
					 && i.ValidUntilUtc > now)
			.ToListAsync(ct);
	}

	/// <inheritdoc/>
	public async Task<Incident?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		return await DbContext.Set<Incident>()
			.FirstOrDefaultAsync(i =>
				i.ExternalId.Value == externalId &&
				i.Source.Value == source &&
				i.CountryCode.Value == countryCode, ct);
	}

	/// <inheritdoc/>
	public async Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default)
	{
		await DbContext.Set<Incident>()
			.Where(i => i.Source.Value == source && i.CountryCode.Value == countryCode)
			.ExecuteDeleteAsync(ct);
	}

	/// <inheritdoc/>
	public async Task DeleteExpiredAsync(CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		await DbContext.Set<Incident>()
			.Where(i => i.ValidUntilUtc < now)
			.ExecuteDeleteAsync(ct);
	}
	#endregion
}
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
/// Purpose     : EF Core implementation of <see cref="IRoadWorkRepository"/>.
///              GetInAreaAsync filters to currently-active records server-side
///              (ValidFromUtc ≤ UtcNow &lt; ValidUntilUtc).
///
///              Note: ExecuteDeleteAsync cannot be used with OwnsOne-mapped properties (Position, Audit)
///              because EF Core includes owned navigations in the translated query, which SQL Server
///              cannot execute as a bulk DELETE. Bulk deletes therefore use raw SQL targeting
///              the underlying scalar columns directly.
/// </summary>
internal sealed class RoadWorkRepository : BaseAsyncCommandRepository<RoadWork, RoadWorkId>, IRoadWorkRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="RoadWorkRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public RoadWorkRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region IRoadWorkRepository
	/// <inheritdoc/>
	public async Task<IReadOnlyList<RoadWork>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await DbContext.Set<RoadWork>()
			.Where(r => r.Position.Latitude >= bbox.South && r.Position.Latitude <= bbox.North
					 && r.Position.Longitude >= bbox.West && r.Position.Longitude <= bbox.East
					 && r.ValidFromUtc <= now
					 && r.ValidUntilUtc > now)
			.ToListAsync(ct);
	}

	/// <inheritdoc/>
	public async Task<RoadWork?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		return await DbContext.Set<RoadWork>()
			.FirstOrDefaultAsync(r =>
				r.ExternalId.Value == externalId &&
				r.Source.Value == source &&
				r.CountryCode.Value == countryCode, ct);
	}

	/// <inheritdoc/>
	public async Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default)
	{
		// ExecuteDeleteAsync cannot be used here — EF Core includes OwnsOne navigations (Position, Audit)
		// in the translated query which SQL Server cannot execute as a bulk DELETE statement.
		// Raw SQL targets the scalar columns directly, bypassing owned entity navigation.
		// ct is passed as a named argument to avoid it being captured into the params object[] array.
		await DbContext.Database.ExecuteSqlRawAsync(
			"DELETE FROM [RoadWorks] WHERE [Source] = {0} AND [CountryCode] = {1}",
			parameters: new object[] { source, countryCode },
			cancellationToken: ct);
	}

	/// <inheritdoc/>
	public async Task DeleteExpiredAsync(CancellationToken ct = default)
	{
		// Same OwnsOne constraint applies — raw SQL required for bulk expired-row cleanup.
		// ct is passed as a named argument to avoid it being captured into the params object[] array.
		await DbContext.Database.ExecuteSqlRawAsync(
			"DELETE FROM [RoadWorks] WHERE [ValidUntilUtc] < {0}",
			parameters: new object[] { DateTime.UtcNow },
			cancellationToken: ct);
	}
	#endregion
}
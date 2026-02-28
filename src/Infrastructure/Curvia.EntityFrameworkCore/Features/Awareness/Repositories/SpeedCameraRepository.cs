using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Shared.ValueObjects;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="ISpeedCameraRepository"/>.
///              SpeedCamera is a trivial aggregate root — single-entity aggregate,
///              own consistency boundary, no child entities, no domain events needed.
///              Spatial queries use a BETWEEN predicate on indexed (Latitude, Longitude)
///              columns — sufficient for SQL Server at pan-European scale without PostGIS.
///              No global IsDeleted query filter: lifecycle is managed by the Worker's
///              DeleteBySourceAsync + reload pattern, not soft delete.
///
///              Note: ExecuteDeleteAsync cannot be used with OwnsOne-mapped properties (Position, Audit)
///              because EF Core includes owned navigations in the translated query, which SQL Server
///              cannot execute as a bulk DELETE. Bulk deletes therefore use raw SQL targeting
///              the underlying scalar columns directly.
/// </summary>
internal sealed class SpeedCameraRepository : BaseAsyncCommandRepository<SpeedCamera, SpeedCameraId>, ISpeedCameraRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="SpeedCameraRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public SpeedCameraRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region ISpeedCameraRepository
	/// <inheritdoc/>
	public async Task<IReadOnlyList<SpeedCamera>> GetInAreaAsync(BoundingBox bbox, CancellationToken ct = default)
	{
		return await DbContext.Set<SpeedCamera>()
			.Where(c => c.Position.Latitude >= bbox.South && c.Position.Latitude <= bbox.North
					 && c.Position.Longitude >= bbox.West && c.Position.Longitude <= bbox.East)
			.ToListAsync(ct);
	}

	/// <inheritdoc/>
	public async Task<SpeedCamera?> FindByExternalIdAsync(string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		// Construct value objects from raw strings so EF Core can apply the registered HasConversion
		// converter on the parameter side and generate a correctly parameterized WHERE clause.
		// Accessing .Value directly inside a LINQ predicate (e.g. c.ExternalId.Value == externalId)
		// is not translatable — EF cannot traverse into the CLR property through the expression tree.
		// Passing the value object itself lets EF use its converter to produce the scalar SQL parameter.
		var externalIdVo = ExternalId.FromPersistence(externalId);
		var sourceVo = SourceName.FromPersistence(source);
		var countryCodeVo = AwarenessCountryCode.FromPersistence(countryCode);

		return await DbContext.Set<SpeedCamera>()
			.FirstOrDefaultAsync(c =>
				c.ExternalId == externalIdVo &&
				c.Source == sourceVo &&
				c.CountryCode == countryCodeVo, ct);
	}

	/// <inheritdoc/>
	public async Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default)
	{
		// ExecuteDeleteAsync cannot be used here — EF Core includes OwnsOne navigations (Position, Audit)
		// in the translated query which SQL Server cannot execute as a bulk DELETE statement.
		// Raw SQL targets the scalar columns directly, bypassing owned entity navigation.
		// ct is passed as a named argument to avoid it being captured into the params object[] array.
		await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM SpeedCameras WHERE Source = {0} AND CountryCode = {1}", cancellationToken: ct, parameters: [source, countryCode]);
	}
	#endregion
}
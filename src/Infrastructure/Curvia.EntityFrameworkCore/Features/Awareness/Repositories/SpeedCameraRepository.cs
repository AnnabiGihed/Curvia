using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

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
		return await DbContext.Set<SpeedCamera>()
			.FirstOrDefaultAsync(c =>
				c.ExternalId.Value == externalId &&
				c.Source.Value == source &&
				c.CountryCode.Value == countryCode, ct);
	}

	/// <inheritdoc/>
	public async Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default)
	{
		await DbContext.Set<SpeedCamera>()
			.Where(c => c.Source.Value == source && c.CountryCode.Value == countryCode)
			.ExecuteDeleteAsync(ct);
	}
	#endregion
}
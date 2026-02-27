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
/// Purpose     : EF Core implementation of <see cref="ISpeedCameraRepository"/>.
///
///              Inherits BaseAsyncCommandRepository for standard Add/Update/Delete.
///              SpeedCamera is a trivial aggregate root — single-entity aggregate,
///              own consistency boundary, no child entities, no domain events needed.
///
///              Spatial queries use a BETWEEN predicate on indexed (Latitude, Longitude)
///              columns — sufficient for SQL Server at pan-European scale without PostGIS.
///
///              No global IsDeleted query filter: lifecycle is managed by the Worker's
///              DeleteBySourceAsync + reload pattern, not soft delete.
/// </summary>
internal sealed class SpeedCameraRepository
	: BaseAsyncCommandRepository<SpeedCamera, SpeedCameraId>, ISpeedCameraRepository
{
	public SpeedCameraRepository(CurviaDbContext dbContext) : base(dbContext) { }

	public async Task<IReadOnlyList<SpeedCamera>> GetInAreaAsync(
		BoundingBox bbox, CancellationToken ct = default)
	{
		return await DbContext.Set<SpeedCamera>()
			.Where(c => c.Latitude >= bbox.South && c.Latitude <= bbox.North
					 && c.Longitude >= bbox.West && c.Longitude <= bbox.East)
			.ToListAsync(ct);
	}

	public async Task<SpeedCamera?> FindByExternalIdAsync(
		string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		return await DbContext.Set<SpeedCamera>()
			.FirstOrDefaultAsync(c =>
				c.ExternalId == externalId &&
				c.Source == source &&
				c.CountryCode == countryCode, ct);
	}

	public async Task DeleteBySourceAsync(
		string source, string countryCode, CancellationToken ct = default)
	{
		await DbContext.Set<SpeedCamera>()
			.Where(c => c.Source == source && c.CountryCode == countryCode)
			.ExecuteDeleteAsync(ct);
	}
}

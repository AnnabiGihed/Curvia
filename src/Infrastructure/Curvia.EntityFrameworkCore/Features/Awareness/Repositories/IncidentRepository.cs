using Curvia.Domain.Features.Awareness.Aggregates;
using Curvia.Domain.Features.Awareness.Repositories;
using Curvia.Domain.Features.Awareness.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Awareness.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IIncidentRepository"/>.
///              GetInAreaAsync filters to active incidents server-side.
/// </summary>
internal sealed class IncidentRepository
	: BaseAsyncCommandRepository<Incident, IncidentId>, IIncidentRepository
{
	public IncidentRepository(CurviaDbContext dbContext) : base(dbContext) { }

	public async Task<IReadOnlyList<Incident>> GetInAreaAsync(
		BoundingBox bbox, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await DbContext.Set<Incident>()
			.Where(i => i.Latitude >= bbox.South && i.Latitude <= bbox.North
					 && i.Longitude >= bbox.West && i.Longitude <= bbox.East
					 && i.ValidFromUtc <= now
					 && i.ValidUntilUtc > now)
			.ToListAsync(ct);
	}

	public async Task<Incident?> FindByExternalIdAsync(
		string externalId, string source, string countryCode, CancellationToken ct = default)
	{
		return await DbContext.Set<Incident>()
			.FirstOrDefaultAsync(i =>
				i.ExternalId == externalId &&
				i.Source == source &&
				i.CountryCode == countryCode, ct);
	}

	public async Task DeleteBySourceAsync(
		string source, string countryCode, CancellationToken ct = default)
	{
		await DbContext.Set<Incident>()
			.Where(i => i.Source == source && i.CountryCode == countryCode)
			.ExecuteDeleteAsync(ct);
	}

	public async Task DeleteExpiredAsync(CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		await DbContext.Set<Incident>()
			.Where(i => i.ValidUntilUtc < now)
			.ExecuteDeleteAsync(ct);
	}
}
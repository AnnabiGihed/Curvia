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
/// Purpose     : EF Core implementation of <see cref="IHazardRepository"/>.
///              Hazard is a trivial aggregate root — single-entity aggregate, no child entities,
///              no soft-delete. Lifecycle is managed by the Worker's DeleteBySourceAsync + reload pattern.
///
///              Note: ExecuteDeleteAsync cannot be used with OwnsOne-mapped properties (Position, Audit)
///              because EF Core internally includes owned navigations in the translated query, which
///              SQL Server cannot execute as a bulk DELETE. Bulk deletes therefore use raw SQL targeting
///              the underlying scalar columns directly.
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
		// Construct value objects from raw strings so EF Core can apply the registered HasConversion
		// converter on the parameter side and generate a correctly parameterized WHERE clause.
		// Accessing .Value directly inside a LINQ predicate (e.g. h.ExternalId.Value == externalId)
		// is not translatable — EF cannot traverse into the CLR property through the expression tree.
		// Passing the value object itself lets EF use its converter to produce the scalar SQL parameter.
		var externalIdVo = ExternalId.FromPersistence(externalId);
		var sourceVo = SourceName.FromPersistence(source);
		var countryCodeVo = AwarenessCountryCode.FromPersistence(countryCode);

		return await DbContext.Set<Hazard>()
			.FirstOrDefaultAsync(h =>
				h.ExternalId == externalIdVo &&
				h.Source == sourceVo &&
				h.CountryCode == countryCodeVo, ct);
	}

	/// <inheritdoc/>
	public async Task DeleteBySourceAsync(string source, string countryCode, CancellationToken ct = default)
	{
		// ExecuteDeleteAsync cannot be used here — EF Core includes OwnsOne navigations (Position, Audit)
		// in the translated query which SQL Server cannot execute as a bulk DELETE statement.
		// Raw SQL targets the scalar columns directly, bypassing owned entity navigation.
		// ct is passed as a named argument to avoid it being captured into the params object[] array.
		await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Hazards WHERE Source = {0} AND CountryCode = {1}", cancellationToken: ct, parameters: [source, countryCode]);
	}
	#endregion
}
using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IMotorcycleMakerRepository"/>.
///              Global query filter (IsDeleted = false) applied automatically.
/// </summary>
internal sealed class MotorcycleMakerRepository : BaseAsyncCommandRepository<MotorcycleMaker, MotorcycleMakerId>, IMotorcycleMakerRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="MotorcycleMakerRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public MotorcycleMakerRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region IMotorcycleMakerRepository
	/// <inheritdoc/>
	public async Task<bool> OfficialNameExistsAsync(string name, CancellationToken ct = default)
	{
		return await DbContext.Set<MotorcycleMaker>()
			.AnyAsync(m => m.Status == CatalogItemStatus.Official && m.Name.Value.ToLower() == name.Trim().ToLower(), ct);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<MotorcycleMaker>> GetAllPendingAsync(CancellationToken ct = default)
	{
		return await DbContext.Set<MotorcycleMaker>()
			.AsNoTracking()
			.Where(m => m.Status == CatalogItemStatus.PendingValidation)
			.OrderBy(m => m.Audit.CreatedOnUtc)
			.ToListAsync(ct);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<MotorcycleMaker>> GetAllOfficialAsync(CancellationToken ct = default)
	{
		return await DbContext.Set<MotorcycleMaker>()
			.AsNoTracking()
			.Where(m => m.Status == CatalogItemStatus.Official)
			.OrderBy(m => m.Name.Value)
			.ToListAsync(ct);
	}
	#endregion
}
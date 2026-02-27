using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IMotorcycleCatalogModelRepository"/>.
///              Global query filter (IsDeleted = false) applied automatically.
/// </summary>
internal sealed class MotorcycleCatalogModelRepository : BaseAsyncCommandRepository<MotorcycleCatalogModel, MotorcycleCatalogModelId>, IMotorcycleCatalogModelRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="MotorcycleCatalogModelRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public MotorcycleCatalogModelRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region IMotorcycleCatalogModelRepository
	/// <inheritdoc/>
	public async Task<IReadOnlyList<MotorcycleCatalogModel>> GetAllPendingAsync(CancellationToken ct = default)
	{
		return await DbContext.Set<MotorcycleCatalogModel>()
			.AsNoTracking()
			.Where(m => m.Status == CatalogItemStatus.PendingValidation)
			.OrderBy(m => m.Audit.CreatedOnUtc)
			.ToListAsync(ct);
	}

	/// <inheritdoc/>
	public async Task<bool> OfficialNameExistsAsync(MotorcycleMakerId makerId, string name, CancellationToken ct = default)
	{
		return await DbContext.Set<MotorcycleCatalogModel>()
			.AnyAsync(m => m.MakerId == makerId && m.Status == CatalogItemStatus.Official && m.Name.Value.ToLower() == name.Trim().ToLower(), ct);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<MotorcycleCatalogModel>> GetOfficialByMakerAsync(MotorcycleMakerId makerId, CancellationToken ct = default)
	{
		return await DbContext.Set<MotorcycleCatalogModel>()
			.AsNoTracking()
			.Where(m => m.MakerId == makerId && m.Status == CatalogItemStatus.Official)
			.OrderBy(m => m.Name)
			.ToListAsync(ct);
	}
	#endregion
}
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Domain.Features.MotorcycleCatalog.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IMotorcycleCatalogModelRepository"/>.
///              Global query filter (IsDeleted = false) applied automatically.
/// </summary>
internal sealed class MotorcycleCatalogModelRepository
	: BaseAsyncCommandRepository<MotorcycleCatalogModel, MotorcycleCatalogModelId>,
	  IMotorcycleCatalogModelRepository
{
	public MotorcycleCatalogModelRepository(CurviaDbContext dbContext) : base(dbContext) { }

	public async Task<IReadOnlyList<MotorcycleCatalogModel>> GetOfficialByMakerAsync(
		MotorcycleMakerId makerId, CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleCatalogModel>()
			.AsNoTracking()
			.Where(m => m.MakerId == makerId && m.Status == CatalogItemStatus.Official)
			.OrderBy(m => m.Name)
			.ToListAsync(ct);

	public async Task<IReadOnlyList<MotorcycleCatalogModel>> GetAllPendingAsync(CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleCatalogModel>()
			.AsNoTracking()
			.Where(m => m.Status == CatalogItemStatus.PendingValidation)
			.OrderBy(m => m.Audit.CreatedOnUtc)
			.ToListAsync(ct);

	public async Task<MotorcycleCatalogModel?> FindByIdAsync(
		MotorcycleCatalogModelId id, CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleCatalogModel>()
			.FirstOrDefaultAsync(m => m.Id == id, ct);

	public async Task<bool> OfficialNameExistsAsync(
		MotorcycleMakerId makerId, string name, CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleCatalogModel>()
			.AnyAsync(m => m.MakerId == makerId
						&& m.Status == CatalogItemStatus.Official
						&& m.Name.ToLower() == name.Trim().ToLower(), ct);
}







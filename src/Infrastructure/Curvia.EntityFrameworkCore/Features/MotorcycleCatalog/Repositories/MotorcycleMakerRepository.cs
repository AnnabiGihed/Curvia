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
/// Purpose     : EF Core implementation of <see cref="IMotorcycleMakerRepository"/>.
///              Global query filter (IsDeleted = false) applied automatically.
/// </summary>
internal sealed class MotorcycleMakerRepository
	: BaseAsyncCommandRepository<MotorcycleMaker, MotorcycleMakerId>, IMotorcycleMakerRepository
{
	public MotorcycleMakerRepository(CurviaDbContext dbContext) : base(dbContext) { }

	public async Task<IReadOnlyList<MotorcycleMaker>> GetAllOfficialAsync(CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleMaker>()
			.AsNoTracking()
			.Where(m => m.Status == CatalogItemStatus.Official)
			.OrderBy(m => m.Name)
			.ToListAsync(ct);

	public async Task<IReadOnlyList<MotorcycleMaker>> GetAllPendingAsync(CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleMaker>()
			.AsNoTracking()
			.Where(m => m.Status == CatalogItemStatus.PendingValidation)
			.OrderBy(m => m.Audit.CreatedOnUtc)
			.ToListAsync(ct);

	public async Task<MotorcycleMaker?> FindByIdAsync(MotorcycleMakerId id, CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleMaker>()
			.FirstOrDefaultAsync(m => m.Id == id, ct);

	public async Task<bool> OfficialNameExistsAsync(string name, CancellationToken ct = default)
		=> await DbContext.Set<MotorcycleMaker>()
			.AnyAsync(m => m.Status == CatalogItemStatus.Official
						&& m.Name.ToLower() == name.Trim().ToLower(), ct);
}
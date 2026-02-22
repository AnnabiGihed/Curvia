using Curvia.Domain.Features.Motorcycles.Aggregates;
using Curvia.Domain.Features.Motorcycles.Repositories;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Motorcycles.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IMotorcycleRepository"/>.
///              Global query filter (IsDeleted = false) is applied automatically.
/// </summary>
internal sealed class MotorcycleRepository
	: BaseAsyncCommandRepository<Motorcycle, MotorcycleId>, IMotorcycleRepository
{
	public MotorcycleRepository(CurviaDbContext dbContext) : base(dbContext) { }

	/// <inheritdoc/>
	public async Task<IReadOnlyList<Motorcycle>> GetByUserIdAsync(
		UserId userId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<Motorcycle>()
			.Where(m => m.UserId == userId)
			.OrderByDescending(m => m.IsDefault)
			.ThenByDescending(m => m.Audit.CreatedOnUtc)
			.ToListAsync(cancellationToken);

	/// <inheritdoc/>
	public async Task<Motorcycle?> FindByIdAndUserAsync(
		MotorcycleId id, UserId userId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<Motorcycle>()
			.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, cancellationToken);

	/// <inheritdoc/>
	public async Task<Motorcycle?> FindDefaultAsync(
		UserId userId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<Motorcycle>()
			.FirstOrDefaultAsync(m => m.UserId == userId && m.IsDefault, cancellationToken);

	/// <inheritdoc/>
	public async Task ClearDefaultAsync(
		UserId userId, MotorcycleId exceptId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<Motorcycle>()
			.Where(m => m.UserId == userId && m.Id != exceptId && m.IsDefault)
			.ExecuteUpdateAsync(
				s => s.SetProperty(m => m.IsDefault, false),
				cancellationToken);
}
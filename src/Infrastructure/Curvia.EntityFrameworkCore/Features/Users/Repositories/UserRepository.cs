using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Users.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IUserQueryRepository"/>.
///              Global query filter on AppUser (IsDeleted = false) is applied
///              automatically by EF — all queries here exclude soft-deleted users.
/// </summary>
internal class UserRepository : BaseAsyncCommandRepository<User, UserId>, IUserRepository
{
	#region Constructor
	public UserRepository(CurviaDbContext dbContext) : base(dbContext)
	{
	}
	#endregion

	#region IAppUserQueryRepository
	/// <inheritdoc/>
	public async Task<User?> FindByKeycloakIdAsync(string keycloakId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<User>().FirstOrDefaultAsync(u => u.KeycloakId == keycloakId, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<User?> FindByIdAsync(UserId id, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<User>().AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
	}
	#endregion
}
using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Users.Repositories;
using Curvia.Domain.Features.Users.ValueObjects;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Users.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core implementation of <see cref="IUserRepository"/>.
///              Global query filter on User (IsDeleted = false) is applied automatically.
/// </summary>
internal sealed class UserRepository : BaseAsyncCommandRepository<User, UserId>, IUserRepository
{
	#region Constructor
	/// <summary>
	/// Initializes a new instance of <see cref="UserRepository"/> backed by <see cref="CurviaDbContext"/>.
	/// </summary>
	/// <param name="dbContext">EF Core database context.</param>
	public UserRepository(CurviaDbContext dbContext) : base(dbContext) { }
	#endregion

	#region IUserRepository
	/// <inheritdoc/>
	public async Task<bool> EmailExistsAsync(UserEmail email, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<User>()
			.AnyAsync(u => u.Email == email, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<User?> FindByKeycloakIdAsync(KeycloakId keycloakId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Set<User>()
			.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId, cancellationToken);
	}
	#endregion
}
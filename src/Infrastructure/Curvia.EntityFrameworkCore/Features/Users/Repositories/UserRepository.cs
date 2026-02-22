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
///
///              VO COMPARISONS:
///              User.KeycloakId and User.Email are value objects mapped with HasConversion.
///              EF Core translates VO equality in LINQ expressions by extracting .Value
///              through the registered converter — the SQL stays a plain string comparison.
/// </summary>
internal sealed class UserRepository : BaseAsyncCommandRepository<User, UserId>, IUserRepository
{
	#region Constructor
	public UserRepository(CurviaDbContext dbContext) : base(dbContext) {}
	#endregion

	#region IUserRepository
	/// <inheritdoc/>
	public async Task<User?> FindByKeycloakIdAsync(KeycloakId keycloakId, CancellationToken cancellationToken = default)
		=> await DbContext.Set<User>()
			.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId, cancellationToken);

	/// <inheritdoc/>
	public async Task<User?> FindByIdAsync(UserId id, CancellationToken cancellationToken = default)
		=> await DbContext.Set<User>()
			.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

	/// <inheritdoc/>
	public async Task<bool> EmailExistsAsync(UserEmail email, CancellationToken cancellationToken = default)
		=> await DbContext.Set<User>()
			.AnyAsync(u => u.Email == email, cancellationToken);
	#endregion
}
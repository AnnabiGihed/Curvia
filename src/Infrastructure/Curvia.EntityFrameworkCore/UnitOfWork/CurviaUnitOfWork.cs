using Microsoft.AspNetCore.Http;
using Pivot.Framework.Infrastructure.Abstraction.UnitOfWork;
using Pivot.Framework.Infrastructure.Abstraction.Outbox.DomainEventPublisher;
using Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore.UnitOfWork;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;

namespace Curvia.Persistence.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Curvia-specific unit of work. Scopes IUnitOfWork to CurviaDbContext
///               for unambiguous DI resolution.
///               All logic lives in the base class UnitOfWork&lt;TContext&gt;:
///               audit stamping, domain event → outbox flushing, and SaveChangesAsync.
///               This class exists only to satisfy the DI discriminator pattern and
///               to supply the strongly-typed CurviaDbContext to the base constructor.
/// </summary>
internal sealed class CurviaUnitOfWork : UnitOfWork<CurviaDbContext>, IUnitOfWork<CurviaDbContext>
{
	public CurviaUnitOfWork(CurviaDbContext dbContext, IHttpContextAccessor httpContextAccessor, IDomainEventPublisher domainEventPublisher) : base(dbContext, httpContextAccessor, domainEventPublisher) { }
}
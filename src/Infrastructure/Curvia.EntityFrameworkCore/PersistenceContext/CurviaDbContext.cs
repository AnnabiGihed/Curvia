using Microsoft.EntityFrameworkCore;
using Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.PersistenceContext;

namespace Curvia.Persistence.EntityFrameworkCore.PersistenceContext;

public class CurviaDbContext : TemplatesCoreDbContextBase
{
	#region Constructor
	public CurviaDbContext(DbContextOptions dbContextOptions)
		:base(dbContextOptions)
	{
		
	}
	#endregion
}

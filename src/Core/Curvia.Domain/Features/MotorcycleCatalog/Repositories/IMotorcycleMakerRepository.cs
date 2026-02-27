using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

namespace Curvia.Domain.Features.MotorcycleCatalog.Repositories;
/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Domain port for MotorcycleMaker persistence.
/// </summary>
public interface IMotorcycleMakerRepository : IAsyncCommandRepository<MotorcycleMaker, MotorcycleMakerId>
{
	/// <summary>
	/// Case-insensitive name check against Official makers.
	/// Used to prevent duplicate suggestions and admin duplicates.
	/// </summary>
	Task<bool> OfficialNameExistsAsync(string name, CancellationToken ct = default);

	/// <summary>
	/// All PendingValidation makers.
	/// Used in the admin approval UI.
	/// </summary>
	Task<IReadOnlyList<MotorcycleMaker>> GetAllPendingAsync(CancellationToken ct = default);

	/// <summary>
	/// All Official makers, sorted alphabetically.
	/// Used for public dropdowns.
	/// </summary>
	Task<IReadOnlyList<MotorcycleMaker>> GetAllOfficialAsync(CancellationToken ct = default);
}
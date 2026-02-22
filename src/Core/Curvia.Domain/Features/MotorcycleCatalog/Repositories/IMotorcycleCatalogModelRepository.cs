using Templates.Core.Domain.Repositories;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

namespace Curvia.Domain.Features.MotorcycleCatalog.Repositories;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Domain port for MotorcycleCatalogModel persistence.
/// </summary>
public interface IMotorcycleCatalogModelRepository : IAsyncCommandRepository<MotorcycleCatalogModel, MotorcycleCatalogModelId>
{
	/// <summary>
	/// All PendingValidation models.
	/// Used in the admin approval UI.
	/// </summary>
	Task<IReadOnlyList<MotorcycleCatalogModel>> GetAllPendingAsync(CancellationToken ct = default);

	/// <summary>
	/// Find by ID (any status).
	/// Returns null if not found or soft-deleted.
	/// </summary>
	Task<MotorcycleCatalogModel?> FindByIdAsync(MotorcycleCatalogModelId id, CancellationToken ct = default);

	/// <summary>
	/// Case-insensitive name check within a maker's Official models.
	/// Used to prevent duplicate suggestions.
	/// </summary>
	Task<bool> OfficialNameExistsAsync(MotorcycleMakerId makerId, string name, CancellationToken ct = default);

	/// <summary>
	/// Official models for a given maker, sorted alphabetically.
	/// Used for public model dropdowns after a maker is selected.
	/// </summary>
	Task<IReadOnlyList<MotorcycleCatalogModel>> GetOfficialByMakerAsync(MotorcycleMakerId makerId, CancellationToken ct = default);
}
using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Domain.Features.MotorcycleCatalog.ValueObjects;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a motorcycle model within a maker's lineup.
///              Aggregate root responsible for lifecycle, moderation workflow,
///              and audit tracking of catalog model entries.
/// </summary>
public sealed class MotorcycleCatalogModel : AggregateRoot<MotorcycleCatalogModelId>
{
	#region Properties
	/// <summary>
	/// Public model name (e.g. "Monster 937", "R 1250 GS Adventure").
	/// Max 150 characters, validated via <see cref="ModelName"/>.
	/// </summary>
	public ModelName Name { get; private set; } = default!;

	/// <summary>Current moderation state (Official or PendingValidation).</summary>
	public CatalogItemStatus Status { get; private set; }

	/// <summary>Null for admin-created or seeded models.</summary>
	public UserId? SuggestedByUserId { get; private set; }

	/// <summary>UI classification used for grouping/filtering models.</summary>
	public MotorcycleCategory Category { get; private set; }

	/// <summary>Cross-aggregate reference to the owning maker.</summary>
	public MotorcycleMakerId MakerId { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>Parameterless constructor for EF Core materialization.</summary>
	private MotorcycleCatalogModel() { }
	#endregion

	#region Factories
	/// <summary>
	/// Admin adds a new official model to the catalog.
	/// Initializes audit information with the provided actor.
	/// </summary>
	/// <param name="makerId">Owning maker identifier.</param>
	/// <param name="name">Model name.</param>
	/// <param name="category">Model category (UI grouping).</param>
	/// <param name="actorId">Actor performing the creation (admin/system).</param>
	/// <returns>Successful result containing <see cref="MotorcycleCatalogModel"/> or failure.</returns>
	public static Result<MotorcycleCatalogModel> Create(MotorcycleMakerId makerId, string name, MotorcycleCategory category, string actorId)
	{
		if (makerId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error("MotorcycleCatalogModel.MakerRequired", "A maker is required."));

		var nameResult = ModelName.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleCatalogModel>(nameResult.Error);

		var model = new MotorcycleCatalogModel
		{
			MakerId = makerId,
			Name = nameResult.Value,
			Category = category,
			SuggestedByUserId = null,
			Id = MotorcycleCatalogModelId.New(),
			Status = CatalogItemStatus.Official,
		};
		model.InitializeAudit(DateTime.UtcNow, actorId);

		return Result.Success(model);
	}

	/// <summary>
	/// User suggests a model not yet in the catalog.
	/// MakerId may reference either an existing Official maker or a newly suggested one.
	/// </summary>
	/// <param name="makerId">Owning maker identifier.</param>
	/// <param name="name">Suggested model name.</param>
	/// <param name="category">Model category.</param>
	/// <param name="suggestedByUserId">User suggesting the model.</param>
	/// <returns>Successful result containing <see cref="MotorcycleCatalogModel"/> or failure.</returns>
	public static Result<MotorcycleCatalogModel> Suggest(MotorcycleMakerId makerId, string name, MotorcycleCategory category, UserId suggestedByUserId)
	{
		if (makerId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error("MotorcycleCatalogModel.MakerRequired", "A maker is required."));

		if (suggestedByUserId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error("MotorcycleCatalogModel.Suggest.UserRequired", "A user ID is required to suggest a model."));

		var nameResult = ModelName.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleCatalogModel>(nameResult.Error);

		var model = new MotorcycleCatalogModel
		{
			MakerId = makerId,
			Name = nameResult.Value,
			Category = category,
			SuggestedByUserId = suggestedByUserId,
			Id = MotorcycleCatalogModelId.New(),
			Status = CatalogItemStatus.PendingValidation,
		};
		model.InitializeAudit(DateTime.UtcNow, suggestedByUserId.Value.ToString());

		return Result.Success(model);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Approves a pending model and promotes it to Official status.
	/// </summary>
	/// <param name="actorId">Admin performing the approval.</param>
	/// <returns>Success, or failure if already official.</returns>
	public Result Approve(string actorId)
	{
		if (Status == CatalogItemStatus.Official)
			return Result.Failure(new Error("MotorcycleCatalogModel.Approve.AlreadyOfficial", "This model is already official."));

		Status = CatalogItemStatus.Official;
		Touch(DateTime.UtcNow, actorId);
		return Result.Success();
	}

	/// <summary>
	/// Rejects and soft-deletes a pending model suggestion.
	/// </summary>
	/// <param name="actorId">Admin performing the rejection.</param>
	public void Reject(string actorId) => SoftDelete(DateTime.UtcNow, actorId);
	#endregion
}
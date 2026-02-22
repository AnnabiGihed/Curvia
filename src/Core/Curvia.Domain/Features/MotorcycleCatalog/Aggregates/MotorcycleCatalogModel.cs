using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;

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
	/// </summary>
	public string Name { get; private set; } = default!;

	/// <summary>
	/// Current moderation state (Official or PendingValidation).
	/// </summary>
	public CatalogItemStatus Status { get; private set; }

	/// <summary>
	/// Null for admin-created or seeded models.
	/// </summary>
	public UserId? SuggestedByUserId { get; private set; }

	/// <summary>
	/// UI classification used for grouping/filtering models.
	/// </summary>
	public MotorcycleCategory Category { get; private set; }

	/// <summary>
	/// Cross-aggregate reference to the owning maker.
	/// </summary>
	public MotorcycleMakerId MakerId { get; private set; } = default!;
	#endregion

	#region Constructor (EF Core)
	/// <summary>
	/// Parameterless constructor for EF Core materialization.
	/// </summary>
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

		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleCatalogModel>(nameResult.Error);

		var model = new MotorcycleCatalogModel
		{
			MakerId = makerId,
			Name = name.Trim(),
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
	/// Status is PendingValidation until an admin approves.
	/// </summary>
	/// <param name="makerId">Owning maker identifier.</param>
	/// <param name="name">Suggested model name.</param>
	/// <param name="category">Suggested model category.</param>
	/// <param name="suggestedByUserId">User suggesting the model.</param>
	/// <returns>Successful result containing <see cref="MotorcycleCatalogModel"/> or failure.</returns>
	public static Result<MotorcycleCatalogModel> Suggest(MotorcycleMakerId makerId, string name, MotorcycleCategory category, UserId suggestedByUserId)
	{
		if (makerId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error("MotorcycleCatalogModel.MakerRequired", "A maker is required."));

		if (suggestedByUserId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error("MotorcycleCatalogModel.Suggest.UserRequired", "A user ID is required to suggest a model."));

		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleCatalogModel>(nameResult.Error);

		var model = new MotorcycleCatalogModel
		{
			MakerId = makerId,
			Name = name.Trim(),
			Category = category,
			Id = MotorcycleCatalogModelId.New(),
			SuggestedByUserId = suggestedByUserId,
			Status = CatalogItemStatus.PendingValidation
		};
		model.InitializeAudit(DateTime.UtcNow, suggestedByUserId.Value.ToString());

		return Result.Success(model);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Soft-deletes the model.
	/// </summary>
	/// <param name="actorId">Actor performing the removal.</param>
	public void Remove(string actorId)
	{
		Delete(DateTime.UtcNow, actorId);
	}

	/// <summary>
	/// Admin approves a pending model, making it visible in public dropdowns.
	/// </summary>
	/// <param name="actorId">Actor performing the approval.</param>
	/// <returns>Failure if already official; otherwise success.</returns>
	public Result Approve(string actorId)
	{
		if (Status == CatalogItemStatus.Official)
			return Result.Failure(new Error("MotorcycleCatalogModel.AlreadyOfficial", "This model is already approved."));

		Status = CatalogItemStatus.Official;
		Touch(DateTime.UtcNow, actorId);

		return Result.Success();
	}

	/// <summary>
	/// Admin corrects the model name or category.
	/// </summary>
	/// <param name="newName">New validated model name.</param>
	/// <param name="newCategory">New category.</param>
	/// <param name="actorId">Actor performing the update.</param>
	/// <returns>Failure if validation fails; otherwise success.</returns>
	public Result Update(string newName, MotorcycleCategory newCategory, string actorId)
	{
		var nameResult = ValidateName(newName);
		if (nameResult.IsFailure)
			return Result.Failure(nameResult.Error);

		Name = newName.Trim();
		Category = newCategory;
		Touch(DateTime.UtcNow, actorId);

		return Result.Success();
	}
	#endregion

	#region Private validation
	/// <summary>
	/// Validates model name constraints (non-empty, max 150 characters).
	/// </summary>
	/// <param name="name">Raw model name.</param>
	/// <returns>Success if valid; otherwise failure.</returns>
	private static Result ValidateName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Result.Failure(new Error("MotorcycleCatalogModel.Name.Empty", "Model name must not be empty."));

		if (name.Trim().Length > 150)
			return Result.Failure(new Error("MotorcycleCatalogModel.Name.TooLong", "Model name must not exceed 150 characters."));

		return Result.Success();
	}
	#endregion
}
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Domain.Features.Users.Aggregate;
using System;
using System.Collections.Generic;
using System.Text;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a motorcycle model within a maker's lineup.
///
///              Named MotorcycleCatalogModel (not MotorcycleModel) to avoid collision
///              with the MotorcycleModel value object in the Motorcycles feature.
///
///              TWO CREATION PATHS:
///              ─────────────────────────────────────────────────────────────────
///              Create()  — Admin adds an official model. Status = Official immediately.
///              Suggest() — User proposes a model not in the list. Status = PendingValidation.
///                          Not shown in public dropdowns until Approve() is called.
///
///              AGGREGATE BOUNDARY:
///              MotorcycleCatalogModel is its own aggregate root (not a child of MotorcycleMaker).
///              The relationship is expressed via MakerId — a cross-aggregate reference by ID.
///              This allows models to be queried, approved, and managed independently.
/// </summary>
public sealed class MotorcycleCatalogModel : AggregateRoot<MotorcycleCatalogModelId>
{
	#region Properties

	/// <summary>Cross-aggregate reference to the owning maker.</summary>
	public MotorcycleMakerId MakerId { get; private set; } = default!;

	public string Name { get; private set; } = default!;
	public MotorcycleCategory Category { get; private set; }
	public CatalogItemStatus Status { get; private set; }

	/// <summary>Null for admin-created or seeded models.</summary>
	public UserId? SuggestedByUserId { get; private set; }

	#endregion

	#region Constructor (EF Core)
	private MotorcycleCatalogModel() { }
	#endregion

	#region Factories

	/// <summary>Admin adds a new official model to the catalog.</summary>
	public static Result<MotorcycleCatalogModel> Create(
		MotorcycleMakerId makerId,
		string name,
		MotorcycleCategory category,
		string actorId)
	{
		if (makerId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error(
				"MotorcycleCatalogModel.MakerRequired",
				"A maker is required."));

		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleCatalogModel>(nameResult.Error);

		var model = new MotorcycleCatalogModel
		{
			Id = MotorcycleCatalogModelId.New(),
			MakerId = makerId,
			Name = name.Trim(),
			Category = category,
			Status = CatalogItemStatus.Official,
			SuggestedByUserId = null,
		};
		model.InitializeAudit(DateTime.UtcNow, actorId);
		return Result.Success(model);
	}

	/// <summary>
	/// User suggests a model not yet in the catalog.
	/// MakerId may reference either an existing Official maker or a newly suggested one.
	/// Status is PendingValidation until an admin approves.
	/// </summary>
	public static Result<MotorcycleCatalogModel> Suggest(
		MotorcycleMakerId makerId,
		string name,
		MotorcycleCategory category,
		UserId suggestedByUserId)
	{
		if (makerId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error(
				"MotorcycleCatalogModel.MakerRequired",
				"A maker is required."));

		if (suggestedByUserId is null)
			return Result.Failure<MotorcycleCatalogModel>(new Error(
				"MotorcycleCatalogModel.Suggest.UserRequired",
				"A user ID is required to suggest a model."));

		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleCatalogModel>(nameResult.Error);

		var model = new MotorcycleCatalogModel
		{
			Id = MotorcycleCatalogModelId.New(),
			MakerId = makerId,
			Name = name.Trim(),
			Category = category,
			Status = CatalogItemStatus.PendingValidation,
			SuggestedByUserId = suggestedByUserId,
		};
		model.InitializeAudit(DateTime.UtcNow, suggestedByUserId.Value.ToString());
		return Result.Success(model);
	}

	#endregion

	#region Domain Behaviours

	/// <summary>Admin approves a pending model, making it visible in public dropdowns.</summary>
	public Result Approve(string actorId)
	{
		if (Status == CatalogItemStatus.Official)
			return Result.Failure(new Error(
				"MotorcycleCatalogModel.AlreadyOfficial",
				"This model is already approved."));

		Status = CatalogItemStatus.Official;
		Touch(DateTime.UtcNow, actorId);
		return Result.Success();
	}

	/// <summary>Admin corrects the model name or category.</summary>
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

	/// <summary>Soft-deletes the model.</summary>
	public void Remove(string actorId) => Delete(DateTime.UtcNow, actorId);

	#endregion

	#region Private validation

	private static Result ValidateName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Result.Failure(new Error(
				"MotorcycleCatalogModel.Name.Empty",
				"Model name must not be empty."));

		if (name.Trim().Length > 150)
			return Result.Failure(new Error(
				"MotorcycleCatalogModel.Name.TooLong",
				"Model name must not exceed 150 characters."));

		return Result.Success();
	}

	#endregion
}
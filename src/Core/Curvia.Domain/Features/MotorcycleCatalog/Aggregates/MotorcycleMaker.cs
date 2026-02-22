using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a motorcycle manufacturer in the catalog.
///
///              TWO CREATION PATHS:
///              ─────────────────────────────────────────────────────────────────
///              Create()  — Admin adds a new official maker. Status = Official immediately.
///              Suggest() — User proposes a maker not in the list. Status = PendingValidation.
///                          Admin calls Approve() to make it Official.
///
///              INVARIANTS:
///              Name must be non-empty, max 100 chars (trimmed).
///              Only Official makers appear in public dropdowns.
///              Soft delete removes the maker from the catalog without destroying history.
/// </summary>
public sealed class MotorcycleMaker : AggregateRoot<MotorcycleMakerId>
{
	#region Properties
	public string Name { get; private set; } = default!;
	public CatalogItemStatus Status { get; private set; }

	/// <summary>
	/// Null for admin-created or seeded makers.
	/// Populated when a user submits a suggestion.
	/// </summary>
	public UserId? SuggestedByUserId { get; private set; }

	#endregion

	#region Constructor (EF Core)
	private MotorcycleMaker() { }
	#endregion

	#region Factories

	/// <summary>
	/// Admin adds a new official maker to the catalog.
	/// </summary>
	public static Result<MotorcycleMaker> Create(string name, string actorId)
	{
		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleMaker>(nameResult.Error);

		var maker = new MotorcycleMaker
		{
			Id = MotorcycleMakerId.New(),
			Name = name.Trim(),
			Status = CatalogItemStatus.Official,
			SuggestedByUserId = null,
		};
		maker.InitializeAudit(DateTime.UtcNow, actorId);
		return Result.Success(maker);
	}

	/// <summary>
	/// User suggests a maker not yet in the catalog.
	/// Status is PendingValidation until an admin approves.
	/// </summary>
	public static Result<MotorcycleMaker> Suggest(string name, UserId suggestedByUserId)
	{
		if (suggestedByUserId is null)
			return Result.Failure<MotorcycleMaker>(new Error(
				"MotorcycleMaker.Suggest.UserRequired",
				"A user ID is required to suggest a maker."));

		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleMaker>(nameResult.Error);

		var maker = new MotorcycleMaker
		{
			Id = MotorcycleMakerId.New(),
			Name = name.Trim(),
			Status = CatalogItemStatus.PendingValidation,
			SuggestedByUserId = suggestedByUserId,
		};
		maker.InitializeAudit(DateTime.UtcNow, suggestedByUserId.Value.ToString());
		return Result.Success(maker);
	}

	#endregion

	#region Domain Behaviours

	/// <summary>Admin approves a pending maker, making it visible in public dropdowns.</summary>
	public Result Approve(string actorId)
	{
		if (Status == CatalogItemStatus.Official)
			return Result.Failure(new Error(
				"MotorcycleMaker.AlreadyOfficial",
				"This maker is already approved."));

		Status = CatalogItemStatus.Official;
		Touch(DateTime.UtcNow, actorId);
		return Result.Success();
	}

	/// <summary>Admin renames a maker (corrects typos, standardises casing).</summary>
	public Result Rename(string newName, string actorId)
	{
		var nameResult = ValidateName(newName);
		if (nameResult.IsFailure)
			return Result.Failure(nameResult.Error);

		Name = newName.Trim();
		Touch(DateTime.UtcNow, actorId);
		return Result.Success();
	}

	/// <summary>Soft-deletes the maker and its associated models will no longer appear.</summary>
	public void Remove(string actorId) => Delete(DateTime.UtcNow, actorId);

	#endregion

	#region Private validation

	private static Result ValidateName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Result.Failure(new Error(
				"MotorcycleMaker.Name.Empty",
				"Maker name must not be empty."));

		if (name.Trim().Length > 100)
			return Result.Failure(new Error(
				"MotorcycleMaker.Name.TooLong",
				"Maker name must not exceed 100 characters."));

		return Result.Success();
	}
	#endregion
}
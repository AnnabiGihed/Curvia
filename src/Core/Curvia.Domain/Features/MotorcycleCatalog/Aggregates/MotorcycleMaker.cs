using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;

namespace Curvia.Domain.Features.MotorcycleCatalog.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a motorcycle manufacturer in the catalog.
///              Aggregate root responsible for lifecycle, moderation workflow,
///              and audit tracking of maker entries.
/// </summary>
public sealed class MotorcycleMaker : AggregateRoot<MotorcycleMakerId>
{
	#region Properties
	/// <summary>
	/// Public manufacturer name (e.g. Ducati, BMW Motorrad).
	/// </summary>
	public string Name { get; private set; } = default!;

	/// <summary>
	/// Current moderation state (Official or PendingValidation).
	/// </summary>
	public CatalogItemStatus Status { get; private set; }

	/// <summary>
	/// Null for admin-created or seeded makers.
	/// Populated when a user submits a suggestion.
	/// </summary>
	public UserId? SuggestedByUserId { get; private set; }
	#endregion

	#region Constructor (EF Core)
	/// <summary>
	/// Parameterless constructor for EF Core materialization.
	/// </summary>
	private MotorcycleMaker() { }
	#endregion

	#region Factories
	/// <summary>
	/// Admin adds a new official maker to the catalog.
	/// Initializes audit information with the provided actor.
	/// </summary>
	/// <param name="name">Manufacturer name.</param>
	/// <param name="actorId">Actor performing the creation (admin/system).</param>
	/// <returns>Successful result containing <see cref="MotorcycleMaker"/> or failure.</returns>
	public static Result<MotorcycleMaker> Create(string name, string actorId)
	{
		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleMaker>(nameResult.Error);

		var maker = new MotorcycleMaker
		{
			Name = name.Trim(),
			SuggestedByUserId = null,
			Id = MotorcycleMakerId.New(),
			Status = CatalogItemStatus.Official,
		};

		maker.InitializeAudit(DateTime.UtcNow, actorId);

		return Result.Success(maker);
	}

	/// <summary>
	/// User suggests a maker not yet in the catalog.
	/// Status is PendingValidation until an admin approves.
	/// </summary>
	/// <param name="name">Suggested manufacturer name.</param>
	/// <param name="suggestedByUserId">User suggesting the maker.</param>
	/// <returns>Successful result containing <see cref="MotorcycleMaker"/> or failure.</returns>
	public static Result<MotorcycleMaker> Suggest(string name, UserId suggestedByUserId)
	{
		if (suggestedByUserId is null)
			return Result.Failure<MotorcycleMaker>(
				new Error("MotorcycleMaker.Suggest.UserRequired", "A user ID is required to suggest a maker."));

		var nameResult = ValidateName(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleMaker>(nameResult.Error);

		var maker = new MotorcycleMaker
		{
			Name = name.Trim(),
			Id = MotorcycleMakerId.New(),
			SuggestedByUserId = suggestedByUserId,
			Status = CatalogItemStatus.PendingValidation,
		};

		maker.InitializeAudit(DateTime.UtcNow, suggestedByUserId.Value.ToString());

		return Result.Success(maker);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Soft-deletes the maker.
	/// Associated models will no longer appear due to query filtering.
	/// </summary>
	/// <param name="actorId">Actor performing the removal.</param>
	public void Remove(string actorId)
	{
		Delete(DateTime.UtcNow, actorId);
	}

	/// <summary>
	/// Admin approves a pending maker, making it visible in public dropdowns.
	/// </summary>
	/// <param name="actorId">Admin performing the approval.</param>
	/// <returns>Failure if already official; otherwise success.</returns>
	public Result Approve(string actorId)
	{
		if (Status == CatalogItemStatus.Official)
			return Result.Failure(
				new Error("MotorcycleMaker.AlreadyOfficial", "This maker is already approved."));

		Status = CatalogItemStatus.Official;
		Touch(DateTime.UtcNow, actorId);

		return Result.Success();
	}

	/// <summary>
	/// Admin renames a maker (corrects typos or standardises casing).
	/// </summary>
	/// <param name="newName">New validated manufacturer name.</param>
	/// <param name="actorId">Actor performing the rename.</param>
	/// <returns>Failure if validation fails; otherwise success.</returns>
	public Result Rename(string newName, string actorId)
	{
		var nameResult = ValidateName(newName);
		if (nameResult.IsFailure)
			return Result.Failure(nameResult.Error);

		Name = newName.Trim();
		Touch(DateTime.UtcNow, actorId);

		return Result.Success();
	}
	#endregion

	#region Private validation
	/// <summary>
	/// Validates maker name constraints (non-empty, max 100 characters).
	/// </summary>
	/// <param name="name">Raw manufacturer name.</param>
	/// <returns>Success if valid; otherwise failure.</returns>
	private static Result ValidateName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Result.Failure(
				new Error("MotorcycleMaker.Name.Empty", "Maker name must not be empty."));

		if (name.Trim().Length > 100)
			return Result.Failure(
				new Error("MotorcycleMaker.Name.TooLong", "Maker name must not exceed 100 characters."));

		return Result.Success();
	}
	#endregion
}
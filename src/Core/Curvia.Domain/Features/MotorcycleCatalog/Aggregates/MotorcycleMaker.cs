using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Domain.Features.MotorcycleCatalog.ValueObjects;

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
	/// Public manufacturer name (e.g. "Ducati", "BMW Motorrad").
	/// Max 100 characters, validated via <see cref="MakerName"/>.
	/// </summary>
	public MakerName Name { get; private set; } = default!;

	/// <summary>Current moderation state (Official or PendingValidation).</summary>
	public CatalogItemStatus Status { get; private set; }

	/// <summary>
	/// Null for admin-created or seeded makers.
	/// Populated when a user submits a suggestion.
	/// </summary>
	public UserId? SuggestedByUserId { get; private set; }
	#endregion

	#region Constructors
	/// <summary>Parameterless constructor for EF Core materialization.</summary>
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
		var nameResult = MakerName.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleMaker>(nameResult.Error);

		var maker = new MotorcycleMaker
		{
			Name = nameResult.Value,
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
			return Result.Failure<MotorcycleMaker>(new Error("MotorcycleMaker.Suggest.UserRequired", "A user ID is required to suggest a maker."));

		var nameResult = MakerName.Create(name);
		if (nameResult.IsFailure)
			return Result.Failure<MotorcycleMaker>(nameResult.Error);

		var maker = new MotorcycleMaker
		{
			Name = nameResult.Value,
			SuggestedByUserId = suggestedByUserId,
			Id = MotorcycleMakerId.New(),
			Status = CatalogItemStatus.PendingValidation,
		};

		maker.InitializeAudit(DateTime.UtcNow, suggestedByUserId.Value.ToString());

		return Result.Success(maker);
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Approves a pending maker and promotes it to Official status.
	/// </summary>
	/// <param name="actorId">Admin performing the approval.</param>
	/// <returns>Success, or failure if already official.</returns>
	public Result Approve(string actorId)
	{
		if (Status == CatalogItemStatus.Official)
			return Result.Failure(new Error("MotorcycleMaker.Approve.AlreadyOfficial", "This maker is already official."));

		Status = CatalogItemStatus.Official;
		Touch(DateTime.UtcNow, actorId);
		return Result.Success();
	}

	/// <summary>
	/// Rejects and soft-deletes a pending maker suggestion.
	/// </summary>
	/// <param name="actorId">Admin performing the rejection.</param>
	public void Reject(string actorId) => SoftDelete(DateTime.UtcNow, actorId);
	#endregion
}
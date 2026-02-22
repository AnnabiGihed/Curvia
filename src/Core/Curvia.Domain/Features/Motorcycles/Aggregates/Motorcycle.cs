using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.ValueObjects;

namespace Curvia.Domain.Features.Motorcycles.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a motorcycle owned by a user.
/// </summary>
public sealed class Motorcycle : AggregateRoot<MotorcycleId>
{
	#region Properties
	/// <summary>
	/// Model year.
	/// Range: 1900 to current year + 2 (pre-orders).
	/// </summary>
	public int Year { get; private set; }

	/// <summary>
	/// Engine displacement in cc.
	/// Optional. 
	/// Range: 50–2500 cc.
	/// </summary>
	public int? EngineCc { get; private set; }

	/// <summary>
	/// When true, this bike is pre-selected on the route generation UI.
	/// Only one motorcycle per user should be the default — invariant enforced at application layer.
	/// </summary>
	public bool IsDefault { get; private set; }

	/// <summary>
	/// User-given nickname.
	/// E.g. "The Beast". 
	/// Optional. 
	/// Max 100 chars.
	/// </summary>
	public Nickname? Nickname { get; private set; }

	/// <summary>
	/// Manufacturer name. 
	/// E.g. "Ducati", "BMW Motorrad".
	/// Max 100 chars.
	/// </summary>
	public Maker Maker { get; private set; } = default!;

	/// <summary>
	/// Owner's application-level ID.
	/// FK to AppUsers table.
	/// </summary>
	public UserId UserId { get; private set; } = default!;

	/// <summary>
	/// Model name. 
	/// E.g. "Monster 937", "R 1250 GS Adventure".
	/// Max 150 chars.
	/// </summary>
	public MotorcycleModel Model { get; private set; } = default!;
	#endregion

	#region Constructors
	/// <summary>
	/// For EF Core.
	/// </summary>
	private Motorcycle() { }

	private Motorcycle(MotorcycleId id, UserId userId, Maker maker, MotorcycleModel model, int year, int? engineCc, Nickname? nickname, bool isDefault)
		: base(id)
	{
		Year = year;
		Maker = maker;
		Model = model;
		UserId = userId;
		EngineCc = engineCc;
		Nickname = nickname;
		IsDefault = isDefault;
	}
	#endregion

	#region Factory
	/// <summary>
	/// Adds a new motorcycle to a user's garage.
	/// </summary>
	public static Result<Motorcycle> Create(UserId userId, string maker, string model, int year, int? engineCc = null, string? nickname = null, bool isDefault = false)
	{
		if (userId is null)
			return Result.Failure<Motorcycle>(new Error("Motorcycle.UserId.Null", "UserId is required."));

		var makerResult = Maker.Create(maker);
		if (makerResult.IsFailure)
			return Result.Failure<Motorcycle>(makerResult.Error, makerResult.ResultExceptionType);

		var modelResult = MotorcycleModel.Create(model);
		if (modelResult.IsFailure)
			return Result.Failure<Motorcycle>(modelResult.Error, modelResult.ResultExceptionType);

		var yearValidation = ValidateYear(year);
		if (yearValidation.IsFailure)
			return Result.Failure<Motorcycle>(yearValidation.Error, yearValidation.ResultExceptionType);

		var engineCcValidation = ValidateEngineCc(engineCc);
		if (engineCcValidation.IsFailure)
			return Result.Failure<Motorcycle>(engineCcValidation.Error, engineCcValidation.ResultExceptionType);

		Nickname? nicknameVo = null;
		if (nickname is not null)
		{
			var nicknameResult = Nickname.Create(nickname);
			if (nicknameResult.IsFailure)
				return Result.Failure<Motorcycle>(nicknameResult.Error, nicknameResult.ResultExceptionType);

			nicknameVo = nicknameResult.Value;
		}

		var motorcycle = new Motorcycle(id: MotorcycleId.New(), userId: userId, maker: makerResult.Value, model: modelResult.Value, year: year, engineCc: engineCc, nickname: nicknameVo, isDefault: isDefault);

		return Result.Success(motorcycle);
	}
	#endregion

	#region Domain behaviours
	/// <summary>
	/// Marks this motorcycle as the default.
	/// Handler must clear siblings first.
	/// </summary>
	public void SetAsDefault()
	{
		IsDefault = true;
	}

	/// <summary>Removes the default flag. Called by handler before setting a new default.</summary>
	public void ClearDefault()
	{
		IsDefault = false;
	}

	/// <summary>
	/// Soft-deletes this motorcycle from the user's garage.
	/// </summary>
	public void Remove(string actorId)
	{
		Delete(DateTime.UtcNow, actorId);
	}

	/// <summary>
	/// Updates the motorcycle's details.
	/// </summary>
	public Result Update(string maker, string model, int year, int? engineCc, string? nickname)
	{
		var makerResult = Maker.Create(maker);
		if (makerResult.IsFailure)
			return Result.Failure(makerResult.Error, makerResult.ResultExceptionType);

		var modelResult = MotorcycleModel.Create(model);
		if (modelResult.IsFailure)
			return Result.Failure(modelResult.Error, modelResult.ResultExceptionType);

		var yearValidation = ValidateYear(year);
		if (yearValidation.IsFailure)
			return Result.Failure(yearValidation.Error, yearValidation.ResultExceptionType);

		var engineCcValidation = ValidateEngineCc(engineCc);
		if (engineCcValidation.IsFailure)
			return Result.Failure(engineCcValidation.Error, engineCcValidation.ResultExceptionType);

		Nickname? nicknameVo = null;
		if (nickname is not null)
		{
			var nicknameResult = Nickname.Create(nickname);
			if (nicknameResult.IsFailure)
				return Result.Failure(nicknameResult.Error, nicknameResult.ResultExceptionType);

			nicknameVo = nicknameResult.Value;
		}

		Year = year;
		EngineCc = engineCc;
		Nickname = nicknameVo;
		Maker = makerResult.Value;
		Model = modelResult.Value;

		return Result.Success();
	}
	#endregion

	#region Private validation
	private static Result ValidateYear(int year)
	{
		var maxYear = DateTime.UtcNow.Year + 2;
		if (year < 1900 || year > maxYear)
			return Result.Failure(new Error("Motorcycle.Year.OutOfRange", $"Year must be between 1900 and {maxYear}."));

		return Result.Success();
	}

	private static Result ValidateEngineCc(int? engineCc)
	{
		if (engineCc.HasValue && (engineCc.Value < 50 || engineCc.Value > 2500))
			return Result.Failure(new Error("Motorcycle.EngineCc.OutOfRange", "Engine displacement must be between 50 cc and 2,500 cc."));

		return Result.Success();
	}
	#endregion
}
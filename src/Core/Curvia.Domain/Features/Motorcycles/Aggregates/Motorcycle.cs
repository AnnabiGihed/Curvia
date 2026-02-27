using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Users.Aggregate;
using Curvia.Domain.Features.Motorcycles.ValueObjects;

namespace Curvia.Domain.Features.Motorcycles.Aggregates;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Represents a motorcycle owned by a user.
///              Enforces validated value objects for year and engine displacement
///              to prevent impossible or implausible data from entering the domain.
/// </summary>
public sealed class Motorcycle : AggregateRoot<MotorcycleId>
{
	#region Properties
	/// <summary>
	/// Model year.
	/// Range: 1900 to current year + 2 (pre-orders).
	/// </summary>
	public ManufactureYear Year { get; private set; } = default!;

	/// <summary>
	/// Engine displacement in cc.
	/// Optional.
	/// Range: 50–2 500 cc.
	/// </summary>
	public EngineDisplacement? EngineCc { get; private set; }

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
	/// <summary>For EF Core.</summary>
	private Motorcycle() { }

	/// <summary>Initialises a <see cref="Motorcycle"/> aggregate.</summary>
	/// <param name="id">Aggregate identifier.</param>
	/// <param name="userId">Owner user identifier.</param>
	/// <param name="maker">Manufacturer name value object.</param>
	/// <param name="model">Model name value object.</param>
	/// <param name="year">Manufacture year value object.</param>
	/// <param name="engineCc">Optional engine displacement value object.</param>
	/// <param name="nickname">Optional user-given nickname.</param>
	/// <param name="isDefault">Whether this motorcycle is the user's default.</param>
	private Motorcycle(MotorcycleId id, UserId userId, Maker maker, MotorcycleModel model, ManufactureYear year, EngineDisplacement? engineCc, Nickname? nickname, bool isDefault) : base(id)
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
	/// <param name="userId">Owner user identifier.</param>
	/// <param name="maker">Manufacturer name (e.g. "Ducati").</param>
	/// <param name="model">Model name (e.g. "Monster 937").</param>
	/// <param name="year">Four-digit model year.</param>
	/// <param name="engineCc">Optional engine displacement in cc.</param>
	/// <param name="nickname">Optional user-given nickname.</param>
	/// <param name="isDefault">Whether this is the user's default motorcycle.</param>
	/// <returns>Successful result containing the created <see cref="Motorcycle"/>; otherwise failure.</returns>
	public static Result<Motorcycle> Create(UserId userId, string maker, string model, int year, int? engineCc = null, string? nickname = null, bool isDefault = false)
	{
		if (userId is null)
			return Result.Failure<Motorcycle>(new Error("Motorcycle.UserId.Null", "UserId is required."));

		var makerResult = Maker.Create(maker);
		if (makerResult.IsFailure)
			return Result.Failure<Motorcycle>(makerResult.Error);

		var modelResult = MotorcycleModel.Create(model);
		if (modelResult.IsFailure)
			return Result.Failure<Motorcycle>(modelResult.Error);

		var yearResult = ManufactureYear.Create(year);
		if (yearResult.IsFailure)
			return Result.Failure<Motorcycle>(yearResult.Error);

		EngineDisplacement? engineDisplacement = null;
		if (engineCc.HasValue)
		{
			var engineResult = EngineDisplacement.Create(engineCc.Value);
			if (engineResult.IsFailure)
				return Result.Failure<Motorcycle>(engineResult.Error);

			engineDisplacement = engineResult.Value;
		}

		Nickname? nicknameVo = null;
		if (nickname is not null)
		{
			var nicknameResult = Nickname.Create(nickname);
			if (nicknameResult.IsFailure)
				return Result.Failure<Motorcycle>(nicknameResult.Error);

			nicknameVo = nicknameResult.Value;
		}

		return Result.Success(new Motorcycle(MotorcycleId.New(), userId, makerResult.Value, modelResult.Value, yearResult.Value, engineDisplacement, nicknameVo, isDefault));
	}
	#endregion

	#region Domain Behaviours
	/// <summary>
	/// Updates the motorcycle's details.
	/// </summary>
	/// <param name="maker">Updated manufacturer name.</param>
	/// <param name="model">Updated model name.</param>
	/// <param name="year">Updated model year.</param>
	/// <param name="engineCc">Updated optional engine displacement in cc.</param>
	/// <param name="nickname">Updated optional nickname.</param>
	/// <returns>Success when updated; otherwise failure.</returns>
	public Result Update(string maker, string model, int year, int? engineCc, string? nickname)
	{
		var makerResult = Maker.Create(maker);
		if (makerResult.IsFailure) return Result.Failure(makerResult.Error);

		var modelResult = MotorcycleModel.Create(model);
		if (modelResult.IsFailure) return Result.Failure(modelResult.Error);

		var yearResult = ManufactureYear.Create(year);
		if (yearResult.IsFailure) return Result.Failure(yearResult.Error);

		EngineDisplacement? engineDisplacement = null;
		if (engineCc.HasValue)
		{
			var engineResult = EngineDisplacement.Create(engineCc.Value);
			if (engineResult.IsFailure) return Result.Failure(engineResult.Error);
			engineDisplacement = engineResult.Value;
		}

		Nickname? nicknameVo = null;
		if (nickname is not null)
		{
			var nicknameResult = Nickname.Create(nickname);
			if (nicknameResult.IsFailure) return Result.Failure(nicknameResult.Error);
			nicknameVo = nicknameResult.Value;
		}

		Maker = makerResult.Value;
		Model = modelResult.Value;
		Year = yearResult.Value;
		EngineCc = engineDisplacement;
		Nickname = nicknameVo;

		return Result.Success();
	}

	/// <summary>
	/// Sets this motorcycle as the user's default.
	/// </summary>
	public void SetAsDefault() => IsDefault = true;

	/// <summary>
	/// Clears the default flag on this motorcycle.
	/// </summary>
	public void ClearDefault() => IsDefault = false;

	/// <summary>
	/// Soft-deletes this motorcycle from the user's garage.
	/// </summary>
	/// <param name="actorId">Actor performing the removal.</param>
	public void Remove(string actorId) => SoftDelete(DateTime.UtcNow, actorId);
	#endregion
}
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a waypoint in a route plan.
/// </summary>
public sealed class Waypoint : CSharpFunctionalExtensions.ValueObject<Waypoint>
{
	#region Properties

	public GeoCoordinate Location { get; }

	#endregion

	#region Constructor
	private Waypoint()
	{
	}
	private Waypoint(GeoCoordinate location)
	{
		Location = location;
	}

	#endregion

	#region Factory

	public static Result<Waypoint> Create(GeoCoordinate location)
	{
		if (location is null)
			return Result.Failure<Waypoint>(Error.NullValue);

		return Result.Success(new Waypoint(location));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(Waypoint other)
		=> Location.Equals(other.Location);

	protected override int GetHashCodeCore()
		=> Location.GetHashCode();

	#endregion
}

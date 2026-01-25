using Templates.Core.Domain.Shared;
using Templates.Core.Domain.Primitives;
using Curvia.Domain.Features.Routing.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.Errors;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Domain.Features.Routing.RoutePlans.Aggregate;


/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Aggregate representing a route planning request (intent + constraints + scoring).
///              It is the consistency boundary for input validation prior to route computation.
/// </summary>
public sealed class RoutePlan : AggregateRoot<RoutePlanId>
{
	#region Fields

	private readonly List<Waypoint> _waypoints = new();

	#endregion

	#region Properties

	public GeoCoordinate Start { get; private set; } = default!;

	/// <summary>
	/// If provided, this is a point-to-point route.
	/// Mutually exclusive with LoopSpec.
	/// </summary>
	public GeoCoordinate? End { get; private set; }

	/// <summary>
	/// If provided, this is a loop route.
	/// Mutually exclusive with End.
	/// </summary>
	public LoopSpec? LoopSpec { get; private set; }

	public IReadOnlyCollection<Waypoint> Waypoints => _waypoints.AsReadOnly();

	public RoutingConstraints Constraints { get; private set; } = default!;
	public ScoringProfile ScoringProfile { get; private set; } = default!;

	#endregion

	#region Constructors

	private RoutePlan(RoutePlanId id) : base(id) { }

	private RoutePlan() { }

	#endregion

	#region Factory

	public static Result<RoutePlan> Create(
		GeoCoordinate start,
		GeoCoordinate? end,
		LoopSpec? loopSpec,
		IReadOnlyCollection<Waypoint>? waypoints,
		RoutingConstraints constraints,
		ScoringProfile scoringProfile)
	{
		if (start is null)
			return Result.Failure<RoutePlan>(RoutingErrors.NullValue(nameof(start)));

		if (constraints is null)
			return Result.Failure<RoutePlan>(RoutingErrors.NullValue(nameof(constraints)));

		if (scoringProfile is null)
			return Result.Failure<RoutePlan>(RoutingErrors.NullValue(nameof(scoringProfile)));

		// Exactly one of End or LoopSpec must be provided
		var hasEnd = end is not null;
		var hasLoop = loopSpec is not null;

		if (!hasEnd && !hasLoop)
			return Result.Failure<RoutePlan>(RoutingPlansErrors.EndOrLoopRequired());

		if (hasEnd && hasLoop)
			return Result.Failure<RoutePlan>(RoutingPlansErrors.EndAndLoopNotAllowed());

		var plan = new RoutePlan(RoutePlanId.New())
		{
			Start = start,
			End = end,
			LoopSpec = loopSpec,
			Constraints = constraints,
			ScoringProfile = scoringProfile
		};

		if (waypoints is not null && waypoints.Count > 0)
		{
			// Domain guardrail (adjust as needed)
			if (waypoints.Count > 25)
				return Result.Failure<RoutePlan>(RoutingPlansErrors.WaypointsTooMany(waypoints.Count));

			foreach (var wp in waypoints)
			{
				if (wp is null)
					return Result.Failure<RoutePlan>(RoutingErrors.NullValue(nameof(waypoints)));

				plan._waypoints.Add(wp);
			}
		}

		return Result.Success(plan);
	}

	#endregion

	#region Domain Behaviors

	public Result AddWaypoint(Waypoint waypoint)
	{
		if (waypoint is null)
			return Result.Failure(RoutingErrors.NullValue(nameof(waypoint)));

		if (_waypoints.Count >= 25)
			return Result.Failure(RoutingPlansErrors.WaypointsTooMany(_waypoints.Count + 1));

		_waypoints.Add(waypoint);
		return Result.Success();
	}

	public Result ClearWaypoints()
	{
		_waypoints.Clear();
		return Result.Success();
	}

	#endregion
}
namespace Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Controls how the return leg of a roundtrip loop is generated.
///              Only relevant when <see cref="LoopSpec"/> is present on a <see cref="RoutePlan"/>.
/// </summary>
public enum ReturnStrategy
{
	/// <summary>
	/// Generates a single circular loop: start → explore → start.
	/// Valhalla treats this as one continuous route where the last location equals the first.
	/// Best for: timed rides, pure exploration, single-loop adventures.
	/// </summary>
	SameRoute = 0,

	/// <summary>
	/// Generates two separate loops using different roads.
	/// The outbound loop is generated first; the return loop is generated with the outbound
	/// road corridor excluded, guaranteeing a genuinely different set of roads on the way back.
	/// Best for: full-day rides where maximum road variety is the goal.
	/// </summary>
	DifferentRoute = 1
}
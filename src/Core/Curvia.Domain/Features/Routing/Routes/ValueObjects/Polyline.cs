using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.Routes.ValueObjects;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents a route geometry polyline as an ordered set of geographic coordinates.
///              Guarantees immutability and enforces at least 2 points.
/// </summary>
public sealed class Polyline : CSharpFunctionalExtensions.ValueObject<Polyline>
{
	#region Fields

	private readonly GeoCoordinate[] _points;

	#endregion

	#region Properties

	public IReadOnlyList<GeoCoordinate> Points => _points;

	#endregion

	#region Constructor
	private Polyline()
	{
	}
	private Polyline(GeoCoordinate[] points)
	{
		_points = points;
	}

	#endregion

	#region Factory

	public static Result<Polyline> Create(IReadOnlyCollection<GeoCoordinate> points)
	{
		if (points is null)
			return Result.Failure<Polyline>(Error.NullValue);

		if (points.Count < 2)
			return Result.Failure<Polyline>(RoutingErrors.PolylineTooFewPoints(points.Count));

		return Result.Success(new Polyline(points.ToArray()));
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(Polyline other)
	{
		if (_points.Length != other._points.Length)
			return false;

		for (var i = 0; i < _points.Length; i++)
		{
			if (!_points[i].Equals(other._points[i]))
				return false;
		}

		return true;
	}

	protected override int GetHashCodeCore()
	{
		var hash = new HashCode();
		hash.Add(_points.Length);

		foreach (var p in _points)
			hash.Add(p);

		return hash.ToHashCode();
	}

	#endregion

	#region Overrides

	public override string ToString() => $"Polyline(Points={_points.Length})";

	#endregion
}

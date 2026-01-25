using System.Globalization;
using Templates.Core.Domain.Shared;

namespace Curvia.Domain.Features.Routing.Shared;

internal static class RoutingErrors
{
	#region Common

	public static Error NullValue(string name)
		=> new(
			"Routing.Common.NullValue",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Common_NullValue, name));

	public static Error NonFiniteNumber(string fieldName)
		=> new(
			"Routing.Common.NonFiniteNumber",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Common_NonFiniteNumber, fieldName));

	#endregion

	#region GeoCoordinate

	public static Error InvalidLatitude(double value)
		=> new(
			"Routing.GeoCoordinate.InvalidLatitude",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Geo_InvalidLatitude, value));

	public static Error InvalidLongitude(double value)
		=> new(
			"Routing.GeoCoordinate.InvalidLongitude",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Geo_InvalidLongitude, value));

	#endregion

	#region Distance

	public static Error DistanceNegative(double meters)
		=> new(
			"Routing.Distance.Negative",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Distance_Negative, meters));

	#endregion

	#region Duration

	public static Error DurationNegative(long seconds)
		=> new(
			"Routing.Duration.Negative",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Duration_Negative, seconds));

	#endregion

	#region Polyline

	public static Error PolylineTooFewPoints(int count)
		=> new(
			"Routing.Polyline.TooFewPoints",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Polyline_TooFewPoints, count));

	#endregion

	#region FunScore

	public static Error FunScoreOutOfRange(double value)
		=> new(
			"Routing.FunScore.OutOfRange",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_FunScore_OutOfRange, value));

	#endregion

	#region ScoringWeights

	public static Error WeightOutOfRange(string fieldName, double value)
		=> new(
			"Routing.ScoringWeights.OutOfRange",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_ScoringWeights_OutOfRange, fieldName, value));

	public static Error WeightsAllZero()
		=> new(
			"Routing.ScoringWeights.AllZero",
			Resource.Routing_ScoringWeights_AllZero);

	#endregion

	#region ScoringProfile

	public static Error InvalidFunFactor(double value)
		=> new(
			"Routing.ScoringProfile.InvalidFunFactor",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_ScoringProfile_InvalidFunFactor, value));

	#endregion

	#region ElevationGain

	public static Error ElevationGainNegative(double meters)
		=> new(
			"Routing.ElevationGain.Negative",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_ElevationGain_Negative, meters));

	#endregion

	#region BoundingBox

	public static Error BoundingBoxInvalid()
		=> new(
			"Routing.BoundingBox.Invalid",
			Resource.Routing_BoundingBox_Invalid);

	public static Error BoundingBoxInverted(string details)
		=> new(
			"Routing.BoundingBox.Inverted",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_BoundingBox_Inverted, details));

	public static Error BoundingBoxTooSmall()
		=> new(
			"Routing.BoundingBox.TooSmall",
			Resource.Routing_BoundingBox_TooSmall);

	#endregion

	#region RoutingConstraints

	public static Error InvalidDetourRatio(double value)
		=> new(
			"Routing.Constraints.InvalidDetourRatio",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_Constraints_InvalidDetourRatio, value));

	#endregion

	#region GraphVersionId

	public static Error GraphVersionIdRequired()
		=> new(
			"Routing.GraphVersionId.Required",
			Resource.Routing_GraphVersionId_Required);

	public static Error GraphVersionIdTooLong(int max)
		=> new(
			"Routing.GraphVersionId.TooLong",
			string.Format(CultureInfo.InvariantCulture, Resource.Routing_GraphVersionId_TooLong, max));

	public static Error GraphVersionIdInvalidChars()
		=> new(
			"Routing.GraphVersionId.InvalidChars",
			Resource.Routing_GraphVersionId_InvalidChars);

	#endregion
}
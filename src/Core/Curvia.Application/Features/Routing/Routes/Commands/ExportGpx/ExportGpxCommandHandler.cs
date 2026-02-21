using System.Xml.Linq;
using Templates.Core.Domain.Shared;
using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Routing.Routes.Commands.ExportGpx;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Builds a valid GPX 1.1 document from the polyline data supplied by the caller.
///              Stateless — no infrastructure dependencies, no database access.
///
///              GPX structure produced:
///                &lt;gpx&gt;
///                  &lt;metadata&gt; — name, description (distance / duration / fun rating), timestamp
///                  &lt;trk&gt;    — outbound track, one &lt;trkseg&gt; with one &lt;trkpt&gt; per polyline point
///                  &lt;trk&gt;    — return leg track (DifferentRoute loops only, omitted otherwise)
///                &lt;/gpx&gt;
///
///              Coordinate encoding:
///                Each &lt;trkpt&gt; carries lat/lon attributes (WGS84 decimal degrees, 6 d.p.).
///                Elevation is omitted — the route polyline from Valhalla is 2D.
///                Timestamp is omitted per-point (not available without real-time tracking).
///
///              Compatible with: Garmin BaseCamp, TwoNav, Kurviger, OsmAnd, Komoot, Google Maps import.
/// </summary>
internal sealed class ExportGpxCommandHandler : ICommandHandler<ExportGpxCommand, ExportGpxResponse>
{
	// GPX 1.1 namespace
	private static readonly XNamespace GpxNs = "http://www.topografix.com/GPX/1/1";

	// Schema instance namespace for xsi:schemaLocation
	private static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";

	/// <inheritdoc/>
	public Task<Result<ExportGpxResponse>> Handle(ExportGpxCommand command, CancellationToken cancellationToken)
	{
		#region Validate polyline
		if (command.Polyline is null || command.Polyline.Count < 2)
		{
			return Task.FromResult(Result.Failure<ExportGpxResponse>(
				new Error("Export.Gpx.InvalidPolyline",
					"The route polyline must contain at least 2 points.")));
		}

		foreach (var pt in command.Polyline)
		{
			if (pt.Length < 2)
			{
				return Task.FromResult(Result.Failure<ExportGpxResponse>(
					new Error("Export.Gpx.MalformedPoint",
						"Each polyline point must be a [latitude, longitude] pair.")));
			}
		}
		#endregion

		var routeName = string.IsNullOrWhiteSpace(command.RouteName) ? "Curvia Route" : command.RouteName.Trim();
		var exportedAt = DateTime.UtcNow;

		#region Build GPX document
		var gpxDoc = new XDocument(
			new XDeclaration("1.0", "UTF-8", null),
			new XElement(GpxNs + "gpx",
				new XAttribute("version", "1.1"),
				new XAttribute("creator", "Curvia — Motorcycle Fun Routing"),
				new XAttribute(XNamespace.Xmlns + "xsi", XsiNs),
				new XAttribute(XsiNs + "schemaLocation",
					"http://www.topografix.com/GPX/1/1 " +
					"http://www.topografix.com/GPX/1/1/gpx.xsd"),

				BuildMetadata(routeName, command, exportedAt),
				BuildTrack(routeName, command.Polyline, command.DistanceMeters,
					command.EstimatedDurationMinutes, command.FunRating, command.FunScore,
					isReturnLeg: false),
				BuildReturnTrack(command)
			)
		);
		#endregion

		var gpxContent = gpxDoc.ToString();
		var fileName = BuildFileName(routeName, exportedAt);

		return Task.FromResult(Result.Success(new ExportGpxResponse
		{
			GpxContent = gpxContent,
			FileName = fileName
		}));
	}

	#region Private — GPX element builders

	/// <summary>
	/// Builds the &lt;metadata&gt; element containing the route name, description and export timestamp.
	/// The description encodes key statistics so they are visible in any GPX reader.
	/// </summary>
	private static XElement BuildMetadata(string routeName, ExportGpxCommand cmd, DateTime exportedAt)
	{
		var distanceKm = cmd.DistanceMeters / 1000.0;
		var hasReturn = HasReturnLeg(cmd);

		var description = hasReturn
			? $"Curvia fun motorcycle route — " +
			  $"Outbound: {distanceKm:F1} km / {cmd.EstimatedDurationMinutes} min / {cmd.FunRating}★ | " +
			  $"Return: {cmd.ReturnLegDistanceMeters / 1000.0:F1} km / {cmd.ReturnLegDurationMinutes} min / {cmd.ReturnLegFunRating}★"
			: $"Curvia fun motorcycle route — " +
			  $"{distanceKm:F1} km / {cmd.EstimatedDurationMinutes} min / {cmd.FunRating}★ fun rating";

		return new XElement(GpxNs + "metadata",
			new XElement(GpxNs + "name", routeName),
			new XElement(GpxNs + "desc", description),
			new XElement(GpxNs + "author",
				new XElement(GpxNs + "name", "Curvia")),
			new XElement(GpxNs + "time", exportedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"))
		);
	}

	/// <summary>
	/// Builds a &lt;trk&gt; element for a single polyline.
	/// Each coordinate pair becomes a &lt;trkpt&gt; with lat/lon attributes (6 decimal places).
	/// A &lt;name&gt; and &lt;desc&gt; element carry human-readable leg metadata.
	/// </summary>
	private static XElement BuildTrack(
		string routeName,
		IReadOnlyList<double[]> polyline,
		double distanceMeters,
		int durationMinutes,
		int funRating,
		double funScore,
		bool isReturnLeg)
	{
		var trackName = isReturnLeg ? $"{routeName} — Return" : routeName;
		var desc = $"{distanceMeters / 1000.0:F1} km · {durationMinutes} min · {funRating}★ (score {funScore:F2})";

		var trackSeg = new XElement(GpxNs + "trkseg",
			polyline.Select(pt => new XElement(GpxNs + "trkpt",
				new XAttribute("lat", pt[0].ToString("F6", System.Globalization.CultureInfo.InvariantCulture)),
				new XAttribute("lon", pt[1].ToString("F6", System.Globalization.CultureInfo.InvariantCulture))
			))
		);

		return new XElement(GpxNs + "trk",
			new XElement(GpxNs + "name", trackName),
			new XElement(GpxNs + "desc", desc),
			new XElement(GpxNs + "type", "Motorcycle"),
			trackSeg
		);
	}

	/// <summary>
	/// Builds the optional return leg &lt;trk&gt; element.
	/// Returns null (no element added) for point-to-point and SameRoute loops.
	/// </summary>
	private static XElement? BuildReturnTrack(ExportGpxCommand cmd)
	{
		if (!HasReturnLeg(cmd))
			return null;

		return BuildTrack(
			cmd.RouteName ?? "Curvia Route",
			cmd.ReturnLegPolyline!,
			cmd.ReturnLegDistanceMeters,
			cmd.ReturnLegDurationMinutes,
			cmd.ReturnLegFunRating,
			funScore: 0.0,   // raw score not forwarded for return leg
			isReturnLeg: true);
	}

	/// <summary>
	/// Returns true if the command includes a valid non-empty return leg polyline.
	/// </summary>
	private static bool HasReturnLeg(ExportGpxCommand cmd)
		=> cmd.ReturnLegPolyline is { Count: >= 2 };

	/// <summary>
	/// Produces a safe, filesystem-friendly filename from the route name and export timestamp.
	/// Example: "curvia-route-2026-02-21.gpx"
	/// </summary>
	private static string BuildFileName(string routeName, DateTime exportedAt)
	{
		var safeName = routeName
			.ToLowerInvariant()
			.Replace(' ', '-')
			.Replace('/', '-')
			.Replace('\\', '-');

		// Strip any remaining characters that are invalid in filenames
		safeName = new string(safeName
			.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
			.ToArray());

		if (string.IsNullOrEmpty(safeName))
			safeName = "curvia-route";

		return $"{safeName}-{exportedAt:yyyy-MM-dd}.gpx";
	}

	#endregion
}
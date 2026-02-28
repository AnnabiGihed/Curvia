using System.Xml.Linq;
using Pivot.Framework.Domain.Shared;
using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Curvia.Application.Features.Routes.Commands.ExportGpx.Responses;

namespace Curvia.Application.Features.Routes.Commands.ExportGpx;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Builds a valid GPX 1.1 document from the polyline data supplied by the caller.
///              Stateless — no infrastructure dependencies, no database access.
///
///              GPX structure produced:
///                &lt;gpx&gt;
///                  &lt;metadata&gt; — name, description (distance / duration / fun rating), timestamp
///                  &lt;trk&gt;    — outbound track with one &lt;trkpt&gt; per polyline point
///                  &lt;trk&gt;    — return leg track (only when ReturnLeg is present and non-empty)
///                &lt;/gpx&gt;
/// </summary>
internal sealed class ExportGpxCommandHandler : ICommandHandler<ExportGpxCommand, ExportGpxResponse>
{
	#region Fields
	private static readonly XNamespace GpxNs = "http://www.topografix.com/GPX/1/1";
	private static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
	#endregion

	#region ICommandHandler
	/// <inheritdoc/>
	public Task<Result<ExportGpxResponse>> Handle(ExportGpxCommand command, CancellationToken cancellationToken)
	{
		#region Validate outbound polyline
		if (command.Polyline is null || command.Polyline.Count < 2)
			return Task.FromResult(Result.Failure<ExportGpxResponse>(new Error("Export.Gpx.InvalidPolyline", "The route polyline must contain at least 2 points.")));

		foreach (var pt in command.Polyline)
			if (pt.Length < 2)
				return Task.FromResult(Result.Failure<ExportGpxResponse>(new Error("Export.Gpx.MalformedPoint", "Each polyline point must be a [latitude, longitude] pair.")));
		#endregion

		var exportedAt = DateTime.UtcNow;
		var hasReturn = command.ReturnLeg?.Polyline is { Count: >= 2 };
		var routeName = string.IsNullOrWhiteSpace(command.RouteName) ? "Curvia Route" : command.RouteName.Trim();

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

				BuildMetadata(routeName, command, hasReturn, exportedAt),

				BuildTrack(
					name: routeName,
					polyline: command.Polyline,
					distanceMeters: command.DistanceMeters,
					durationMinutes: command.EstimatedDurationMinutes,
					funRating: command.FunRating,
					funScore: command.FunScore,
					isReturnLeg: false),

				hasReturn
					? BuildTrack(
						name: routeName,
						polyline: command.ReturnLeg!.Polyline,
						distanceMeters: command.ReturnLeg.DistanceMeters,
						durationMinutes: command.ReturnLeg.EstimatedDurationMinutes,
						funRating: command.ReturnLeg.FunRating,
						funScore: command.ReturnLeg.FunScore,
						isReturnLeg: true)
					: null
			)
		);
		#endregion

		var gpxContent = gpxDoc.ToString();
		var fileName = BuildFileName(routeName, exportedAt);

		return Task.FromResult(Result.Success(new ExportGpxResponse
		{
			FileName = fileName,
			GpxContent = gpxContent
		}));
	}
	#endregion

	#region Private — GPX element builders
	private static string BuildFileName(string routeName, DateTime exportedAt)
	{
		var safeName = new string(
			routeName.ToLowerInvariant()
				.Replace(' ', '-')
				.Replace('/', '-')
				.Replace('\\', '-')
				.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
				.ToArray());

		if (string.IsNullOrEmpty(safeName))
			safeName = "curvia-route";

		return $"{safeName}-{exportedAt:yyyy-MM-dd}.gpx";
	}
	private static XElement BuildMetadata(string routeName, ExportGpxCommand cmd, bool hasReturn, DateTime exportedAt)
	{
		var distKm = cmd.DistanceMeters / 1000.0;

		var description = hasReturn
			? $"Curvia fun motorcycle route — " +
			  $"Outbound: {distKm:F1} km / {cmd.EstimatedDurationMinutes} min / {cmd.FunRating}★ | " +
			  $"Return: {cmd.ReturnLeg!.DistanceMeters / 1000.0:F1} km / {cmd.ReturnLeg.EstimatedDurationMinutes} min / {cmd.ReturnLeg.FunRating}★"
			: $"Curvia fun motorcycle route — " +
			  $"{distKm:F1} km / {cmd.EstimatedDurationMinutes} min / {cmd.FunRating}★ fun rating";

		return new XElement(GpxNs + "metadata",
			new XElement(GpxNs + "name", routeName),
			new XElement(GpxNs + "desc", description),
			new XElement(GpxNs + "author",
				new XElement(GpxNs + "name", "Curvia")),
			new XElement(GpxNs + "time", exportedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"))
		);
	}
	private static XElement BuildTrack(string name, IReadOnlyList<double[]> polyline, double distanceMeters, int durationMinutes, int funRating, double funScore, bool isReturnLeg)
	{
		var trackName = isReturnLeg ? $"{name} — Return" : name;
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
	#endregion
}
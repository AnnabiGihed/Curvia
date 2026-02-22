namespace Curvia.Application.Features.Routes.Commands.ExportGpx.Responses;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Response returned by <see cref="ExportGpxCommandHandler"/>.
///              Contains the raw GPX 1.1 XML document as a UTF-8 string
///              and a suggested download filename for the HTTP response.
/// </summary>
public sealed record ExportGpxResponse
{
	/// <summary>
	/// The complete GPX 1.1 XML document as a UTF-8 string.
	/// Ready to be written directly to an HTTP response with content-type <c>application/gpx+xml</c>.
	/// </summary>
	public string GpxContent { get; init; } = string.Empty;

	/// <summary>
	/// Suggested filename for the Content-Disposition header.
	/// Example: <c>curvia-route-2026-02-21.gpx</c>
	/// </summary>
	public string FileName { get; init; } = string.Empty;
}
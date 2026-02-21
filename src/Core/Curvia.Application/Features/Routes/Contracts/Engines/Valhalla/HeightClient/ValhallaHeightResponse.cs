namespace Curvia.Application.Features.Routes.Contracts.Engines.Valhalla.HeightClient;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Response from Valhalla's /height endpoint.
///              Contains an elevation value in meters for each requested shape point.
/// </summary>
/// <param name="Height">
/// Array of elevation values in meters (WGS84 orthometric), one per input shape point.
/// </param>
public sealed record ValhallaHeightResponse(IReadOnlyList<double> Height);
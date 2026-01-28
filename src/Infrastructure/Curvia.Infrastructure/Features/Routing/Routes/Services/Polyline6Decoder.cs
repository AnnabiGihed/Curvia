using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Infrastructure.Features.Routing.Routes.Services;

internal static class Polyline6Decoder
{
	// Valhalla uses polyline6 by default (precision 1e6) for "shape".
	public static List<GeoCoordinate> Decode(string encoded)
	{
		var points = new List<GeoCoordinate>(512);

		long lat = 0;
		long lon = 0;
		var index = 0;

		while (index < encoded.Length)
		{
			lat += DecodeNextValue(encoded, ref index);
			lon += DecodeNextValue(encoded, ref index);

			var latitude = lat / 1_000_000.0;
			var longitude = lon / 1_000_000.0;

			var gc = GeoCoordinate.Create(latitude, longitude);
			if (gc.IsFailure)
				throw new InvalidOperationException($"Decoded polyline contains invalid coordinate: {gc.Error}");

			points.Add(gc.Value);
		}

		return points;
	}

	private static long DecodeNextValue(string encoded, ref int index)
	{
		long result = 0;
		var shift = 0;

		while (true)
		{
			var b = encoded[index++] - 63;
			result |= ((long)(b & 0x1F) << shift);
			shift += 5;

			if (b < 0x20)
				break;
		}

		var delta = ((result & 1) != 0) ? ~(result >> 1) : (result >> 1);
		return delta;
	}
}

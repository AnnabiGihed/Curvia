using Curvia.Domain.Features.Awareness.ValueObjects;

namespace Curvia.Infrastructure.Features.Awareness.Providers.Osm;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Provides approximate WGS84 bounding boxes for European countries.
///              Used by OSM Overpass providers to scope queries per country.
///              Coordinates are deliberately generous — a few degrees of padding
///              is acceptable for Overpass queries and avoids edge-case misses
///              near country borders (e.g. speed cameras in border towns).
///
///              Keys are ISO 3166-1 alpha-2 country codes.
/// </summary>
internal static class CountryBoundingBoxes
{
	private static readonly Dictionary<string, (double S, double W, double N, double E)> _boxes = new(
		StringComparer.OrdinalIgnoreCase)
	{
		// Benelux
		["BE"] = (49.5, 2.5, 51.6, 6.5),
		["NL"] = (50.7, 3.2, 53.6, 7.3),
		["LU"] = (49.4, 5.7, 50.2, 6.6),
		// DACH
		["DE"] = (47.2, 5.8, 55.1, 15.1),
		["AT"] = (46.4, 9.5, 49.1, 17.2),
		["CH"] = (45.8, 5.9, 47.9, 10.5),
		// France
		["FR"] = (41.3, -5.2, 51.2, 9.7),
		// Iberian Peninsula
		["ES"] = (35.9, -9.4, 43.8, 4.4),
		["PT"] = (36.8, -9.6, 42.2, -6.2),
		// British Isles
		["GB"] = (49.9, -8.7, 60.9, 1.8),
		["IE"] = (51.3, -10.7, 55.4, -5.9),
		// Scandinavia
		["SE"] = (55.3, 10.9, 69.1, 24.2),
		["NO"] = (57.9, 4.4, 71.2, 31.2),
		["DK"] = (54.5, 8.0, 57.8, 15.2),
		["FI"] = (59.8, 19.1, 70.1, 31.6),
		// Baltics
		["EE"] = (57.5, 21.8, 59.7, 28.2),
		["LV"] = (55.7, 20.9, 58.1, 28.2),
		["LT"] = (53.9, 20.9, 56.5, 26.9),
		// Central-Eastern Europe
		["PL"] = (49.0, 14.1, 54.9, 24.2),
		["CZ"] = (48.5, 12.1, 51.1, 18.9),
		["SK"] = (47.7, 16.8, 49.6, 22.6),
		["HU"] = (45.7, 16.1, 48.6, 22.9),
		["RO"] = (43.6, 20.3, 48.3, 29.7),
		["BG"] = (41.2, 22.4, 44.2, 28.6),
		// Balkans
		["HR"] = (42.4, 13.5, 46.6, 19.5),
		["SI"] = (45.4, 13.4, 46.9, 16.6),
		["RS"] = (41.9, 18.8, 46.2, 23.0),
		["BA"] = (42.6, 15.7, 45.3, 19.6),
		["ME"] = (41.8, 18.4, 43.6, 20.4),
		["MK"] = (40.9, 20.5, 42.4, 23.0),
		["AL"] = (39.6, 19.3, 42.7, 21.1),
		// Greece + Cyprus
		["GR"] = (34.8, 19.4, 41.8, 29.6),
		["CY"] = (34.6, 32.3, 35.7, 34.6),
		// Baltic / Nordic islands
		["IS"] = (63.3, -24.6, 66.6, -13.5),
		// Italy + Malta
		["IT"] = (36.6, 6.6, 47.1, 18.6),
		["MT"] = (35.8, 14.2, 36.1, 14.6),
	};

	/// <summary>
	/// Tries to get the bounding box for the given country code.
	/// Returns false when the country is not in the registry.
	/// </summary>
	public static bool TryGet(string countryCode, out BoundingBox bbox)
	{
		bbox = default!;
		if (!_boxes.TryGetValue(countryCode, out var t))
			return false;

		var result = BoundingBox.Create(t.S, t.W, t.N, t.E);
		if (result.IsFailure)
			return false;

		bbox = result.Value;
		return true;
	}

	/// <summary>Returns all registered country codes.</summary>
	public static IReadOnlyList<string> AllCountryCodes => _boxes.Keys.ToList();
}
using System.Text.Json;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Configurations.Converters;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core <see cref="ValueConverter{TModel,TProvider}"/> for persisting <see cref="Polyline"/>
///              as a JSON string and restoring it back to a valid domain value object.
///              Serialization stores an array of latitude/longitude points; deserialization validates
///              each persisted coordinate and the resulting polyline.
/// </summary>
internal sealed class PolylineJsonConverter : ValueConverter<Polyline, string>
{
	private sealed record PointDto(double Latitude, double Longitude);

	private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

	#region Constructor
	/// <summary>
	/// Initializes a new JSON converter for <see cref="Polyline"/> using custom serialize/deserialize functions.
	/// </summary>
	public PolylineJsonConverter()
		: base(polyline => Serialize(polyline), json => Deserialize(json))
	{
	}
	#endregion

	#region Private Methods
	/// <summary>
	/// Deserializes a JSON string into a validated <see cref="Polyline"/>.
	/// Throws <see cref="InvalidOperationException"/> when persisted data is invalid.
	/// </summary>
	/// <param name="json">JSON payload representing an array of points.</param>
	/// <returns>A valid <see cref="Polyline"/> instance.</returns>
	private static Polyline Deserialize(string json)
	{
		var dtos = JsonSerializer.Deserialize<PointDto[]>(json, Options) ?? Array.Empty<PointDto>();
		var points = new List<GeoCoordinate>(dtos.Length);

		foreach (var dto in dtos)
		{
			var gc = GeoCoordinate.Create(dto.Latitude, dto.Longitude);
			if (gc.IsFailure)
				throw new InvalidOperationException($"Invalid GeoCoordinate persisted in Polyline JSON. {gc.Error}");

			points.Add(gc.Value);
		}

		var pl = Polyline.Create(points);
		if (pl.IsFailure)
			throw new InvalidOperationException($"Invalid Polyline persisted in JSON. {pl.Error}");

		return pl.Value;
	}

	/// <summary>
	/// Serializes a <see cref="Polyline"/> into a JSON string representation.
	/// </summary>
	/// <param name="polyline">Polyline to serialize.</param>
	/// <returns>JSON payload representing an array of points.</returns>
	private static string Serialize(Polyline polyline)
	{
		var dtos = polyline.Points.Select(p => new PointDto(p.Latitude, p.Longitude)).ToArray();
		return JsonSerializer.Serialize(dtos, Options);
	}
	#endregion
}
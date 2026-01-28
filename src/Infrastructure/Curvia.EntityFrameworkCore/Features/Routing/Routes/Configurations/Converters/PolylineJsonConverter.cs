using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Configurations.Converters;

internal sealed class PolylineJsonConverter : ValueConverter<Polyline, string>
{
	private sealed record PointDto(double Latitude, double Longitude);

	private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

	public PolylineJsonConverter()
		: base(
			polyline => Serialize(polyline),
			json => Deserialize(json))
	{
	}

	private static string Serialize(Polyline polyline)
	{
		var dtos = polyline.Points.Select(p => new PointDto(p.Latitude, p.Longitude)).ToArray();
		return JsonSerializer.Serialize(dtos, Options);
	}

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
}
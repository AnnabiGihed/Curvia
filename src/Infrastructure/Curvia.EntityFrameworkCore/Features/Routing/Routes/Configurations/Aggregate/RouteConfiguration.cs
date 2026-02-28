using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Routing.Routes.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Configurations.Converters;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Configurations.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core configuration for <see cref="Route"/> aggregate.
///              Maps the aggregate root, flattens owned value objects (Stats, BoundingBox),
///              configures owned collection <see cref="RouteSegment"/> (including geometry JSON),
///              and persists route geometry as JSON via <see cref="PolylineJsonConverter"/>.
/// </summary>
internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
	#region IEntityTypeConfiguration<Route>
	/// <summary>
	/// Configures EF Core mapping for <see cref="Route"/>:
	/// table name, keys, value object conversions, owned types flattening,
	/// and owned collection mapping for segments.
	/// </summary>
	/// <param name="builder">Entity type builder for <see cref="Route"/>.</param>
	public void Configure(EntityTypeBuilder<Route> builder)
	{
		builder.ToTable(DbTableNames.Routes);

		#region Keys
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, value => new RouteId(value));
		#endregion

		#region Properties - GraphVersionId
		builder.Property(x => x.GraphVersionId)
			.HasConversion(vo => vo.Value, value => GraphVersionId.Create(value).Value)
			.HasMaxLength(128)
			.IsRequired();
		#endregion

		#region Properties - Stats (flattened)
		builder.OwnsOne(x => x.Stats, stats =>
		{
			stats.OwnsOne(p => p.Distance, dist =>
			{
				dist.Property(d => d.Meters).HasColumnName("DistanceMeters").IsRequired();
			});

			stats.OwnsOne(p => p.EstimatedDuration, d =>
			{
				d.Property(x => x.Seconds).HasColumnName("DurationSeconds");
			});

			stats.OwnsOne(p => p.ElevationGain, e =>
			{
				e.Property(x => x.Meters).HasColumnName("ElevationGainMeters");
			});

			stats.OwnsOne(p => p.FunScore, f =>
			{
				f.Property(x => x.Value).HasColumnName("FunScore");
			});
		});
		#endregion

		#region Properties - FK to RoutePlanId
		builder.Property(x => x.RoutePlanId)
			.HasConversion(id => id.Value, value => new RoutePlanId(value))
			.IsRequired();
		#endregion

		#region Segments (owned entity collection)
		builder.Navigation(x => x.Segments)
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.OwnsMany(x => x.Segments, s =>
		{
			s.ToTable(DbTableNames.RouteSegments);
			s.WithOwner()
				.HasForeignKey("RouteId");
			s.Property(x => x.Id)
				.ValueGeneratedNever()
				.HasConversion(id => id.Value, value => new RouteSegmentId(value));
			s.HasKey(x => x.Id);

			s.Property(x => x.Geometry)
				.HasConversion(new PolylineJsonConverter())
				.HasColumnType("nvarchar(max)")
				.IsRequired();

			s.OwnsOne(x => x.Stats, stats =>
			{
				stats.OwnsOne(p => p.Distance, dist =>
				{
					dist.Property(d => d.Meters).HasColumnName("SegmentDistanceMeters").IsRequired();
				});
				stats.OwnsOne(p => p.EstimatedDuration, d =>
				{
					d.Property(x => x.Seconds).HasColumnName("SegmentDurationSeconds");
				});
				stats.OwnsOne(p => p.ElevationGain, e =>
				{
					e.Property(x => x.Meters).HasColumnName("SegmentElevationGainMeters");
				});
				stats.OwnsOne(p => p.FunScore, f =>
				{
					f.Property(x => x.Value).HasColumnName("SegmentFunScore");
				});
			});
		});
		#endregion

		#region Properties - BoundingBox (flattened)
		// Column names updated from Min/MaxLatitude/Longitude → BBoxSouth/West/North/East
		// to align with the canonical South/West/North/East property naming on BoundingBox.
		// ⚠️  A database migration is required to rename these four columns.
		builder.OwnsOne(x => x.BoundingBox, bbox =>
		{
			bbox.Property(p => p.South).HasColumnName("BBoxSouth").IsRequired();
			bbox.Property(p => p.West).HasColumnName("BBoxWest").IsRequired();
			bbox.Property(p => p.North).HasColumnName("BBoxNorth").IsRequired();
			bbox.Property(p => p.East).HasColumnName("BBoxEast").IsRequired();
		});
		#endregion

		#region Properties - Geometry (Polyline as JSON)
		builder.Property(x => x.Geometry)
			.HasConversion(new PolylineJsonConverter())
			.HasColumnType("nvarchar(max)")
			.IsRequired();
		#endregion
	}
	#endregion
}
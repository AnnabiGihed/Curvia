using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.Routing.Routes.Entities;
using Curvia.Domain.Features.Routing.Routes.Aggregate;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.Routing.Routes.ValueObjects;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Configurations.Converters;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.Routes.Configurations.Aggregate;

internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
	public void Configure(EntityTypeBuilder<Route> builder)
	{
		builder.ToTable(DbTableNames.Routes);

		#region Keys

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new RouteId(value));

		#endregion

		#region Properties - FK to RoutePlanId

		builder.Property(x => x.RoutePlanId)
			.HasConversion(
				id => id.Value,
				value => new RoutePlanId(value))
			.IsRequired();

		#endregion

		#region Properties - GraphVersionId

		builder.Property(x => x.GraphVersionId)
			.HasConversion(
				vo => vo.Value,
				value => GraphVersionId.Create(value).Value)
			.HasMaxLength(128)
			.IsRequired();

		#endregion

		#region Properties - Geometry (Polyline as JSON)

		builder.Property(x => x.Geometry)
			.HasConversion(new PolylineJsonConverter())
			.HasColumnType("nvarchar(max)")
			.IsRequired();

		#endregion

		#region Properties - BoundingBox (flattened)

		builder.OwnsOne(x => x.BoundingBox, bbox =>
		{
			bbox.Property(p => p.MinLatitude).HasColumnName("MinLatitude").IsRequired();
			bbox.Property(p => p.MinLongitude).HasColumnName("MinLongitude").IsRequired();
			bbox.Property(p => p.MaxLatitude).HasColumnName("MaxLatitude").IsRequired();
			bbox.Property(p => p.MaxLongitude).HasColumnName("MaxLongitude").IsRequired();
		});

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
				.HasConversion(
					id => id.Value,
					value => new RouteSegmentId(value));
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
	}
}

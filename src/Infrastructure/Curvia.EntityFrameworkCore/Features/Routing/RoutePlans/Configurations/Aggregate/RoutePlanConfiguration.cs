using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.RoutePlans.Configurations.Aggregate;

internal sealed class RoutePlanConfiguration : IEntityTypeConfiguration<RoutePlan>
{
	public void Configure(EntityTypeBuilder<RoutePlan> builder)
	{
		builder.ToTable(DbTableNames.RoutePlans);

		#region Keys

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(
				id => id.Value,
				value => new RoutePlanId(value));

		#endregion

		#region Properties - Start / End

		builder.OwnsOne(x => x.Start, start =>
		{
			start.Property(p => p.Latitude)
				.HasColumnName("StartLatitude")
				.IsRequired();

			start.Property(p => p.Longitude)
				.HasColumnName("StartLongitude")
				.IsRequired();
		});

		builder.OwnsOne(x => x.End, end =>
		{
			end.Property(p => p.Latitude)
				.HasColumnName("EndLatitude");

			end.Property(p => p.Longitude)
				.HasColumnName("EndLongitude");
		});
		#endregion

		#region Properties - LoopSpec (optional)
		builder.OwnsOne(x => x.LoopSpec, loop =>
		{
			loop.Property(l => l.IsLoop)
				.HasColumnName("IsLoop")
				.IsRequired(); // This is the sentinel property

			loop.OwnsOne(p => p.TargetDistance, dist =>
			{
				dist.Property(d => d.Meters)
					.HasColumnName("LoopTargetDistanceMeters");
			});
		});
		#endregion

		#region Properties - Constraints

		builder.OwnsOne(x => x.Constraints, constraints =>
		{
			constraints.Property(p => p.MaxDetourRatio)
				.HasColumnName("MaxDetourRatio")
				.IsRequired();

			constraints.Property(p => p.AvoidHighways)
				.HasColumnName("AvoidHighways")
				.IsRequired();

			constraints.Property(p => p.AvoidTolls)
				.HasColumnName("AvoidTolls")
				.IsRequired();

			constraints.OwnsOne(p => p.MaxDistance, maxDist =>
			{
				maxDist.Property(d => d.Meters)
					.HasColumnName("MaxDistanceMeters");
			});
		});

		#endregion

		#region Properties - ScoringProfile

		builder.OwnsOne(x => x.ScoringProfile, scoring =>
		{
			scoring.Property(p => p.FunFactor)
				.HasColumnName("FunFactor")
				.IsRequired();

			scoring.OwnsOne(p => p.Weights, weights =>
			{
				weights.Property(w => w.Curves)
					.HasColumnName("WeightCurves")
					.IsRequired();

				weights.Property(w => w.Elevation)
					.HasColumnName("WeightElevation")
					.IsRequired();

				weights.Property(w => w.Scenery)
					.HasColumnName("WeightScenery")
					.IsRequired();
			});
		});

		#endregion

		#region Waypoints (owned collection)
		builder.Navigation(x => x.Waypoints)
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.OwnsMany(x => x.Waypoints, w =>
		{
			w.ToTable(DbTableNames.RoutePlanWaypoints);
			w.WithOwner()
				.HasForeignKey("RoutePlanId");
			w.Property<Guid>("Id");
			w.HasKey("Id");
			w.OwnsOne(p => p.Location, loc =>
			{
				loc.Property(x => x.Latitude).HasColumnName("Latitude").IsRequired();
				loc.Property(x => x.Longitude).HasColumnName("Longitude").IsRequired();
			});
		});
		#endregion
	}
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.Routing.Shared.ValueObjects;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;
using Curvia.Domain.Features.Routing.RoutePlans.ValueObjects;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.RoutePlans.Configurations.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core entity type configuration for the <see cref="RoutePlan"/> aggregate.
///              Maps all owned value objects (Start, End, LoopSpec, Constraints, ScoringProfile, Waypoints)
///              to the RoutePlans table using owned-type conventions.
///
///              Changes in this revision:
///                - <see cref="RoutingConstraints.MaxDetourRatio"/> mapped with VO conversion
///                  (was raw double; underlying column type unchanged).
///                - <see cref="ScoringWeights"/> inner weights mapped with <see cref="ScoringWeight"/>
///                  VO conversion (was raw double; underlying column types unchanged).
///                - <see cref="RoutingConstraints.AvoidFerries"/> column added.
///                - <see cref="RoutingConstraints.AvoidMotorwayLinks"/> column added.
/// </summary>
internal sealed class RoutePlanConfiguration : IEntityTypeConfiguration<RoutePlan>
{
	#region IEntityTypeConfiguration<RoutePlan>
	/// <summary>
	/// Configures EF Core mapping for <see cref="RoutePlan"/>:
	/// table name, primary key, and owned types flattening for waypoints, coordinates,
	/// constraints, scoring profile, and loop specification.
	/// </summary>
	/// <param name="builder">Entity type builder for <see cref="RoutePlan"/>.</param>
	public void Configure(EntityTypeBuilder<RoutePlan> builder)
	{
		builder.ToTable(DbTableNames.RoutePlans);

		#region Primary Key
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Id)
			.ValueGeneratedNever()
			.HasConversion(id => id.Value, value => new RoutePlanId(value));
		#endregion

		#region Properties — Waypoints
		builder.OwnsMany(x => x.Waypoints, waypoint =>
		{
			waypoint.ToTable(DbTableNames.RoutePlanWaypoints);
			waypoint.WithOwner().HasForeignKey("RoutePlanId");
			waypoint.Property<Guid>("Id").ValueGeneratedOnAdd();
			waypoint.HasKey("Id");

			waypoint.OwnsOne(w => w.Location, loc =>
			{
				loc.Property(p => p.Latitude).HasColumnName("Latitude").IsRequired();
				loc.Property(p => p.Longitude).HasColumnName("Longitude").IsRequired();
			});
		});
		#endregion

		#region Properties — Start / End
		builder.OwnsOne(x => x.Start, start =>
		{
			start.Property(p => p.Latitude).HasColumnName("StartLatitude").IsRequired();
			start.Property(p => p.Longitude).HasColumnName("StartLongitude").IsRequired();
		});

		builder.OwnsOne(x => x.End, end =>
		{
			end.Property(p => p.Latitude).HasColumnName("EndLatitude");
			end.Property(p => p.Longitude).HasColumnName("EndLongitude");
		});
		#endregion

		#region Properties — Constraints
		builder.OwnsOne(x => x.Constraints, constraints =>
		{
			// MaxDetourRatio is now a typed VO — map via value conversion (column type unchanged: FLOAT)
			constraints.Property(p => p.MaxDetourRatio)
				.IsRequired()
				.HasConversion(vo => vo.Value, raw => MaxDetourRatio.FromPersistence(raw))
				.HasColumnName("MaxDetourRatio")
				.HasColumnType("FLOAT");

			constraints.Property(p => p.AvoidHighways)
				.IsRequired()
				.HasColumnName("AvoidHighways");

			constraints.Property(p => p.AvoidTolls)
				.IsRequired()
				.HasColumnName("AvoidTolls");

			constraints.Property(p => p.AvoidFerries)
				.IsRequired()
				.HasColumnName("AvoidFerries");

			constraints.Property(p => p.AvoidUnpaved)
				.IsRequired()
				.HasColumnName("AvoidUnpaved");

			constraints.Property(p => p.AvoidMotorwayLinks)
				.IsRequired()
				.HasColumnName("AvoidMotorwayLinks");

			// UrbanTolerance stored as INT (enum ordinal)
			constraints.Property(p => p.UrbanTolerance)
				.IsRequired()
				.HasConversion<int>()
				.HasColumnName("UrbanTolerance");

			constraints.Property(p => p.MaxDurationSeconds)
				.IsRequired(false)
				.HasColumnName("MaxDurationSeconds");

			constraints.OwnsOne(p => p.MaxDistance, maxDist =>
			{
				maxDist.Property(d => d.Meters).HasColumnName("MaxDistanceMeters");
			});
		});
		#endregion

		#region Properties — ScoringProfile
		builder.OwnsOne(x => x.ScoringProfile, profile =>
		{
			profile.Property(p => p.FunFactor)
				.IsRequired()
				.HasColumnName("FunFactor")
				.HasColumnType("FLOAT");

			// ScoringWeights now holds ScoringWeight VOs internally — map each with VO conversion
			// Column names unchanged from the original schema (WeightCurves, WeightElevation, WeightScenery)
			profile.OwnsOne(p => p.Weights, weights =>
			{
				weights.Property(w => w.CurvesWeight)
					.IsRequired()
					.HasConversion(vo => vo.Value, raw => ScoringWeight.FromPersistence(raw))
					.HasColumnName("WeightCurves")
					.HasColumnType("FLOAT");

				weights.Property(w => w.ElevationWeight)
					.IsRequired()
					.HasConversion(vo => vo.Value, raw => ScoringWeight.FromPersistence(raw))
					.HasColumnName("WeightElevation")
					.HasColumnType("FLOAT");

				weights.Property(w => w.SceneryWeight)
					.IsRequired()
					.HasConversion(vo => vo.Value, raw => ScoringWeight.FromPersistence(raw))
					.HasColumnName("WeightScenery")
					.HasColumnType("FLOAT");
			});
		});
		#endregion

		#region Properties — LoopSpec (optional)
		builder.OwnsOne(x => x.LoopSpec, loop =>
		{
			loop.Property(l => l.IsLoop)
				.IsRequired()
				.HasColumnName("IsLoop");

			loop.Property(l => l.ReturnStrategy)
				.HasConversion<int>()
				.HasColumnName("LoopReturnStrategy");

			loop.OwnsOne(p => p.TargetDistance, dist =>
			{
				dist.Property(d => d.Meters).HasColumnName("LoopTargetDistanceMeters");
			});
		});
		#endregion
	}
	#endregion
}
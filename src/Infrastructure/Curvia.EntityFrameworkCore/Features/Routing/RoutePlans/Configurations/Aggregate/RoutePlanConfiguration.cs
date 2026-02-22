using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Curvia.Persistence.EntityFrameworkCore.Constants;
using Curvia.Domain.Features.Routing.RoutePlans.Aggregate;

namespace Curvia.Persistence.EntityFrameworkCore.Features.Routing.RoutePlans.Configurations.Aggregate;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : EF Core entity type configuration for the <see cref="RoutePlan"/> aggregate.
///              Maps all owned value objects (Start, End, LoopSpec, Constraints, ScoringProfile, Waypoints)
///              to the RoutePlans table using owned-type conventions.
///              New columns added: IsLoop/ReturnStrategy, MaxDurationSeconds,
///              AvoidUnpaved, UrbanTolerance.
/// </summary>
internal sealed class RoutePlanConfiguration : IEntityTypeConfiguration<RoutePlan>
{
	#region IEntityTypeConfiguration<RoutePlan>
	/// <summary>
	/// Configures EF Core mapping for <see cref="RoutePlan"/>:
	/// table name, primary key, and owned types flattening (waypoints, start/end, constraints,
	/// scoring profile, and optional loop specification).
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

			waypoint.Property<Guid>("Id")
				.ValueGeneratedOnAdd();

			waypoint.HasKey("Id");

			waypoint.OwnsOne(w => w.Location, loc =>
			{
				loc.Property(p => p.Latitude).HasColumnName("Latitude").IsRequired();
				loc.Property(p => p.Longitude).HasColumnName("Longitude").IsRequired();
			});
		});
		#endregion

		#region Properties — Start / End
		builder.OwnsOne(x => x.End, end =>
		{
			end.Property(p => p.Latitude)
				.HasColumnName("EndLatitude");

			end.Property(p => p.Longitude)
				.HasColumnName("EndLongitude");
		});

		builder.OwnsOne(x => x.Start, start =>
		{
			start.Property(p => p.Latitude)
				.HasColumnName("StartLatitude")
				.IsRequired();

			start.Property(p => p.Longitude)
				.HasColumnName("StartLongitude")
				.IsRequired();
		});
		#endregion

		#region Properties — Constraints
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

			constraints.Property(p => p.AvoidUnpaved)
				.HasColumnName("AvoidUnpaved")
				.IsRequired();

			// Stored as int (enum value)
			constraints.Property(p => p.UrbanTolerance)
				.HasColumnName("UrbanTolerance")
				.HasConversion<int>()
				.IsRequired();

			constraints.Property(p => p.MaxDurationSeconds)
				.HasColumnName("MaxDurationSeconds")
				.IsRequired(false);

			constraints.OwnsOne(p => p.MaxDistance, maxDist =>
			{
				maxDist.Property(d => d.Meters)
					.HasColumnName("MaxDistanceMeters");
			});
		});

		#endregion

		#region Properties — ScoringProfile
		builder.OwnsOne(x => x.ScoringProfile, profile =>
		{
			profile.Property(p => p.FunFactor)
				.HasColumnName("FunFactor")
				.IsRequired();

			profile.OwnsOne(p => p.Weights, weights =>
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

		#region Properties — LoopSpec (optional)
		builder.OwnsOne(x => x.LoopSpec, loop =>
		{
			loop.Property(l => l.IsLoop)
				.HasColumnName("IsLoop")
				.IsRequired();

			loop.Property(l => l.ReturnStrategy)
				.HasColumnName("LoopReturnStrategy")
				.HasConversion<int>();

			loop.OwnsOne(p => p.TargetDistance, dist =>
			{
				dist.Property(d => d.Meters)
					.HasColumnName("LoopTargetDistanceMeters");
			});
		});
		#endregion
	}
	#endregion
}
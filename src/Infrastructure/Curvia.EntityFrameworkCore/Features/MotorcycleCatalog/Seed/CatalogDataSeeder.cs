using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Curvia.Domain.Features.MotorcycleCatalog.Enums;
using Curvia.Domain.Features.MotorcycleCatalog.Aggregates;
using Curvia.Persistence.EntityFrameworkCore.PersistenceContext;

namespace Curvia.Persistence.EntityFrameworkCore.Features.MotorcycleCatalog.Seed;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Seeds the official motorcycle maker/model catalog on first startup.
/// </summary>
public class CatalogDataSeeder
{
	private const string SystemActor = "system:seed";

	public static async Task SeedAsync(CurviaDbContext db, ILogger logger, CancellationToken ct = default)
	{
		// Idempotent guard — skip entirely if any official maker already exists
		if (await db.Set<MotorcycleMaker>().AnyAsync(m => m.Status == CatalogItemStatus.Official, ct))
		{
			logger.LogInformation("Motorcycle catalog already seeded — skipping.");
			return;
		}

		logger.LogInformation("Seeding motorcycle catalog…");

		var makers = BuildMakers();
		await db.Set<MotorcycleMaker>().AddRangeAsync(makers.Values.Select(t => t.Maker), ct);
		await db.SaveChangesAsync(ct);

		var models = BuildModels(makers);
		await db.Set<MotorcycleCatalogModel>().AddRangeAsync(models, ct);
		await db.SaveChangesAsync(ct);

		logger.LogInformation(
			"Motorcycle catalog seeded: {MakerCount} makers, {ModelCount} models.",
			makers.Count, models.Count);
	}

	// ── Maker table ───────────────────────────────────────────────────────────

	private static Dictionary<string, (MotorcycleMaker Maker, MotorcycleMakerId Id)> BuildMakers()
	{
		var names = new[]
		{
			"Aprilia", "Benelli", "BMW Motorrad", "Ducati", "Harley-Davidson",
			"Honda", "Husqvarna", "Indian", "Kawasaki", "KTM",
			"Moto Guzzi", "MV Agusta", "Royal Enfield", "Suzuki", "Triumph", "Yamaha",
		};

		var result = new Dictionary<string, (MotorcycleMaker, MotorcycleMakerId)>();

		foreach (var name in names)
		{
			var maker = MotorcycleMaker.Create(name, SystemActor).Value;
			result[name] = (maker, maker.Id);
		}

		return result;
	}

	// ── Model table ───────────────────────────────────────────────────────────

	private static List<MotorcycleCatalogModel> BuildModels(
		Dictionary<string, (MotorcycleMaker Maker, MotorcycleMakerId Id)> makers)
	{
		var models = new List<MotorcycleCatalogModel>();

		void Add(string makerName, string modelName, MotorcycleCategory cat)
		{
			var makerId = makers[makerName].Id;
			models.Add(MotorcycleCatalogModel.Create(makerId, modelName, cat, SystemActor).Value);
		}

		// ── Aprilia ───────────────────────────────────────────────────────────
		Add("Aprilia", "RS 457", MotorcycleCategory.Sport);
		Add("Aprilia", "RS 660", MotorcycleCategory.Sport);
		Add("Aprilia", "RSV4", MotorcycleCategory.Sport);
		Add("Aprilia", "Tuono 660", MotorcycleCategory.Naked);
		Add("Aprilia", "Tuono V4", MotorcycleCategory.Naked);
		Add("Aprilia", "Shiver 900", MotorcycleCategory.Naked);
		Add("Aprilia", "Dorsoduro 900", MotorcycleCategory.Naked);
		Add("Aprilia", "Tuareg 660", MotorcycleCategory.Adventure);

		// ── Benelli ───────────────────────────────────────────────────────────
		Add("Benelli", "752S", MotorcycleCategory.Naked);
		Add("Benelli", "Leoncino 500", MotorcycleCategory.Scrambler);
		Add("Benelli", "TRK 502", MotorcycleCategory.Adventure);

		// ── BMW Motorrad ──────────────────────────────────────────────────────
		Add("BMW Motorrad", "CE 04", MotorcycleCategory.Electric);
		Add("BMW Motorrad", "F 850 GS", MotorcycleCategory.Adventure);
		Add("BMW Motorrad", "F 900 R", MotorcycleCategory.Naked);
		Add("BMW Motorrad", "F 900 XR", MotorcycleCategory.Adventure);
		Add("BMW Motorrad", "G 310 GS", MotorcycleCategory.Adventure);
		Add("BMW Motorrad", "G 310 R", MotorcycleCategory.Naked);
		Add("BMW Motorrad", "M 1000 RR", MotorcycleCategory.Sport);
		Add("BMW Motorrad", "R 1250 GS", MotorcycleCategory.Adventure);
		Add("BMW Motorrad", "R 1250 GS Adventure", MotorcycleCategory.Adventure);
		Add("BMW Motorrad", "R 1250 R", MotorcycleCategory.Naked);
		Add("BMW Motorrad", "R 1250 RT", MotorcycleCategory.Touring);
		Add("BMW Motorrad", "R nineT", MotorcycleCategory.Retro);
		Add("BMW Motorrad", "R nineT Scrambler", MotorcycleCategory.Scrambler);
		Add("BMW Motorrad", "S 1000 RR", MotorcycleCategory.Sport);
		Add("BMW Motorrad", "S 1000 XR", MotorcycleCategory.Adventure);

		// ── Ducati ────────────────────────────────────────────────────────────
		Add("Ducati", "Desert Sled", MotorcycleCategory.Scrambler);
		Add("Ducati", "Diavel V4", MotorcycleCategory.Cruiser);
		Add("Ducati", "Hypermotard 698", MotorcycleCategory.Naked);
		Add("Ducati", "Monster 937", MotorcycleCategory.Naked);
		Add("Ducati", "Monster SP", MotorcycleCategory.Naked);
		Add("Ducati", "Multistrada V4", MotorcycleCategory.Adventure);
		Add("Ducati", "Multistrada V4 Rally", MotorcycleCategory.Adventure);
		Add("Ducati", "Panigale V2", MotorcycleCategory.Sport);
		Add("Ducati", "Panigale V4", MotorcycleCategory.Sport);
		Add("Ducati", "Panigale V4 S", MotorcycleCategory.Sport);
		Add("Ducati", "Scrambler Icon", MotorcycleCategory.Scrambler);
		Add("Ducati", "Streetfighter V2", MotorcycleCategory.Naked);
		Add("Ducati", "Streetfighter V4", MotorcycleCategory.Naked);

		// ── Harley-Davidson ───────────────────────────────────────────────────
		Add("Harley-Davidson", "Fat Boy", MotorcycleCategory.Cruiser);
		Add("Harley-Davidson", "LiveWire", MotorcycleCategory.Electric);
		Add("Harley-Davidson", "Low Rider S", MotorcycleCategory.Cruiser);
		Add("Harley-Davidson", "Nightster", MotorcycleCategory.Cruiser);
		Add("Harley-Davidson", "Pan America 1250", MotorcycleCategory.Adventure);
		Add("Harley-Davidson", "Road Glide", MotorcycleCategory.Touring);
		Add("Harley-Davidson", "Sportster S", MotorcycleCategory.Cruiser);
		Add("Harley-Davidson", "Street Glide", MotorcycleCategory.Touring);

		// ── Honda ─────────────────────────────────────────────────────────────
		Add("Honda", "Africa Twin", MotorcycleCategory.Adventure);
		Add("Honda", "Africa Twin Adventure Sports", MotorcycleCategory.Adventure);
		Add("Honda", "CB125R", MotorcycleCategory.Naked);
		Add("Honda", "CB500F", MotorcycleCategory.Naked);
		Add("Honda", "CB500X", MotorcycleCategory.Adventure);
		Add("Honda", "CB650R", MotorcycleCategory.Naked);
		Add("Honda", "CB1000R", MotorcycleCategory.Naked);
		Add("Honda", "CBR650R", MotorcycleCategory.Sport);
		Add("Honda", "CBR1000RR-R Fireblade", MotorcycleCategory.Sport);
		Add("Honda", "CMX500 Rebel", MotorcycleCategory.Cruiser);
		Add("Honda", "CMX1100 Rebel", MotorcycleCategory.Cruiser);
		Add("Honda", "Gold Wing", MotorcycleCategory.Touring);
		Add("Honda", "NC750X", MotorcycleCategory.Adventure);
		Add("Honda", "NT1100", MotorcycleCategory.Touring);
		Add("Honda", "XL750 Transalp", MotorcycleCategory.Adventure);

		// ── Husqvarna ─────────────────────────────────────────────────────────
		Add("Husqvarna", "Norden 901", MotorcycleCategory.Adventure);
		Add("Husqvarna", "Svartpilen 401", MotorcycleCategory.Scrambler);
		Add("Husqvarna", "Svartpilen 801", MotorcycleCategory.Scrambler);
		Add("Husqvarna", "Vitpilen 801", MotorcycleCategory.Naked);

		// ── Indian ────────────────────────────────────────────────────────────
		Add("Indian", "Chief", MotorcycleCategory.Cruiser);
		Add("Indian", "Chieftain", MotorcycleCategory.Touring);
		Add("Indian", "FTR 1200", MotorcycleCategory.Naked);
		Add("Indian", "Pursuit", MotorcycleCategory.Touring);
		Add("Indian", "Scout", MotorcycleCategory.Cruiser);
		Add("Indian", "Springfield", MotorcycleCategory.Touring);

		// ── Kawasaki ──────────────────────────────────────────────────────────
		Add("Kawasaki", "Ninja 650", MotorcycleCategory.Sport);
		Add("Kawasaki", "Ninja 1000SX", MotorcycleCategory.Sport);
		Add("Kawasaki", "Ninja ZX-6R", MotorcycleCategory.Sport);
		Add("Kawasaki", "Ninja ZX-10R", MotorcycleCategory.Sport);
		Add("Kawasaki", "Versys 650", MotorcycleCategory.Adventure);
		Add("Kawasaki", "Versys 1000", MotorcycleCategory.Adventure);
		Add("Kawasaki", "Vulcan S", MotorcycleCategory.Cruiser);
		Add("Kawasaki", "Z650", MotorcycleCategory.Naked);
		Add("Kawasaki", "Z900", MotorcycleCategory.Naked);
		Add("Kawasaki", "Z900RS", MotorcycleCategory.Retro);
		Add("Kawasaki", "Z900RS Cafe", MotorcycleCategory.Retro);
		Add("Kawasaki", "Z1000", MotorcycleCategory.Naked);

		// ── KTM ───────────────────────────────────────────────────────────────
		Add("KTM", "390 Duke", MotorcycleCategory.Naked);
		Add("KTM", "450 Rally", MotorcycleCategory.Enduro);
		Add("KTM", "790 Adventure", MotorcycleCategory.Adventure);
		Add("KTM", "890 Adventure", MotorcycleCategory.Adventure);
		Add("KTM", "890 Duke", MotorcycleCategory.Naked);
		Add("KTM", "RC 390", MotorcycleCategory.Sport);
		Add("KTM", "1290 Super Adventure S", MotorcycleCategory.Adventure);
		Add("KTM", "1290 Super Duke GT", MotorcycleCategory.Touring);
		Add("KTM", "1290 Super Duke R", MotorcycleCategory.Naked);
		Add("KTM", "1390 Super Duke R", MotorcycleCategory.Naked);

		// ── Moto Guzzi ────────────────────────────────────────────────────────
		Add("Moto Guzzi", "California 1400", MotorcycleCategory.Cruiser);
		Add("Moto Guzzi", "Stelvio", MotorcycleCategory.Adventure);
		Add("Moto Guzzi", "V7", MotorcycleCategory.Retro);
		Add("Moto Guzzi", "V85 TT", MotorcycleCategory.Adventure);
		Add("Moto Guzzi", "V9 Bobber", MotorcycleCategory.Cruiser);

		// ── MV Agusta ─────────────────────────────────────────────────────────
		Add("MV Agusta", "Brutale 1000 RS", MotorcycleCategory.Naked);
		Add("MV Agusta", "Dragster 800 RR", MotorcycleCategory.Naked);
		Add("MV Agusta", "F3 800", MotorcycleCategory.Sport);
		Add("MV Agusta", "Rush 1000", MotorcycleCategory.Naked);
		Add("MV Agusta", "Turismo Veloce 800", MotorcycleCategory.Touring);

		// ── Royal Enfield ─────────────────────────────────────────────────────
		Add("Royal Enfield", "Continental GT 650", MotorcycleCategory.Retro);
		Add("Royal Enfield", "Himalayan 450", MotorcycleCategory.Adventure);
		Add("Royal Enfield", "Hunter 350", MotorcycleCategory.Naked);
		Add("Royal Enfield", "Interceptor 650", MotorcycleCategory.Retro);
		Add("Royal Enfield", "Meteor 350", MotorcycleCategory.Cruiser);

		// ── Suzuki ────────────────────────────────────────────────────────────
		Add("Suzuki", "GSX-8S", MotorcycleCategory.Naked);
		Add("Suzuki", "GSX-S1000", MotorcycleCategory.Naked);
		Add("Suzuki", "GSX-R750", MotorcycleCategory.Sport);
		Add("Suzuki", "GSX-R1000", MotorcycleCategory.Sport);
		Add("Suzuki", "Hayabusa", MotorcycleCategory.Sport);
		Add("Suzuki", "Katana", MotorcycleCategory.Naked);
		Add("Suzuki", "SV650", MotorcycleCategory.Naked);
		Add("Suzuki", "V-Strom 650", MotorcycleCategory.Adventure);
		Add("Suzuki", "V-Strom 800DE", MotorcycleCategory.Adventure);
		Add("Suzuki", "V-Strom 1050", MotorcycleCategory.Adventure);

		// ── Triumph ───────────────────────────────────────────────────────────
		Add("Triumph", "Bonneville T100", MotorcycleCategory.Retro);
		Add("Triumph", "Bonneville T120", MotorcycleCategory.Retro);
		Add("Triumph", "Daytona 660", MotorcycleCategory.Sport);
		Add("Triumph", "Rocket 3", MotorcycleCategory.Cruiser);
		Add("Triumph", "Scrambler 1200 XE", MotorcycleCategory.Scrambler);
		Add("Triumph", "Speed Triple 1200 RS", MotorcycleCategory.Naked);
		Add("Triumph", "Speed Twin 900", MotorcycleCategory.Retro);
		Add("Triumph", "Street Triple R", MotorcycleCategory.Naked);
		Add("Triumph", "Street Triple RS", MotorcycleCategory.Naked);
		Add("Triumph", "Thruxton RS", MotorcycleCategory.Retro);
		Add("Triumph", "Tiger 900", MotorcycleCategory.Adventure);
		Add("Triumph", "Tiger 1200", MotorcycleCategory.Adventure);

		// ── Yamaha ────────────────────────────────────────────────────────────
		Add("Yamaha", "MT-03", MotorcycleCategory.Naked);
		Add("Yamaha", "MT-07", MotorcycleCategory.Naked);
		Add("Yamaha", "MT-09", MotorcycleCategory.Naked);
		Add("Yamaha", "MT-10", MotorcycleCategory.Naked);
		Add("Yamaha", "R1", MotorcycleCategory.Sport);
		Add("Yamaha", "R6", MotorcycleCategory.Sport);
		Add("Yamaha", "R7", MotorcycleCategory.Sport);
		Add("Yamaha", "Ténéré 700", MotorcycleCategory.Adventure);
		Add("Yamaha", "Ténéré 700 Extreme", MotorcycleCategory.Adventure);
		Add("Yamaha", "Tracer 7", MotorcycleCategory.Touring);
		Add("Yamaha", "Tracer 9", MotorcycleCategory.Touring);
		Add("Yamaha", "XSR700", MotorcycleCategory.Retro);
		Add("Yamaha", "XSR900", MotorcycleCategory.Retro);

		return models;
	}
}
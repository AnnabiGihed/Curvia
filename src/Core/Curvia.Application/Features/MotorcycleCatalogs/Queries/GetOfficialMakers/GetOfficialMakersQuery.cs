using Templates.Core.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialMakers;

/// <summary>
/// Returns all Official makers sorted alphabetically.
/// Used for public dropdowns.
/// </summary>
public sealed record GetOfficialMakersQuery : IQuery<IReadOnlyList<MakerDto>>;
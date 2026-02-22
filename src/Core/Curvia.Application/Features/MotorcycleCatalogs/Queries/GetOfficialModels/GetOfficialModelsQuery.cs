using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Templates.Core.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialModels;

/// <summary>
/// Returns Official models for the given maker ID.
/// Used for public dropdowns.
/// </summary>
public sealed record GetOfficialModelsQuery(Guid MakerId) : IQuery<IReadOnlyList<ModelDto>>;
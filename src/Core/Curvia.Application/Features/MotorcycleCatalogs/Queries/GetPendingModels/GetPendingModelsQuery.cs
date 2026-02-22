using Templates.Core.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingModels;

/// <summary>Admin: returns all models awaiting approval, with their maker name resolved.</summary>
public sealed record GetPendingModelsQuery : IQuery<IReadOnlyList<PendingModelDto>>;

using Templates.Core.Application.Abstractions.Messaging.Queries;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;

namespace Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingMakers;

/// <summary>Admin: returns all makers awaiting approval.</summary>
public sealed record GetPendingMakersQuery : IQuery<IReadOnlyList<PendingMakerDto>>;

using Curvia.Application.Features.Motorcycles.Responses;
using Pivot.Framework.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.Motorcycles.Queries.GetByUser;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Returns all active motorcycles in the authenticated user's garage,
///              ordered by IsDefault descending then addition date descending
///              (default bike always first in the list).
/// </summary>
/// <param name="UserId">Authenticated user's application-level identifier.</param>
public sealed record GetUserMotorcyclesQuery(Guid UserId) : IQuery<IReadOnlyList<MotorcycleDto>>;
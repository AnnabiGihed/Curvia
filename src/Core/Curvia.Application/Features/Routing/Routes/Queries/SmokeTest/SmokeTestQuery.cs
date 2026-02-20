using Templates.Core.Domain.Shared;
using Templates.Core.Application.Abstractions.Messaging.Queries;

namespace Curvia.Application.Features.Routing.Routes.Queries.SmokeTest;

public sealed record ValhallaSmokeTestQuery() : IQuery<ValhallaSmokeTestResponse>;
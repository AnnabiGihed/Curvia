using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncHazards;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Command that triggers a sync of permanent road-hazard data for a given country.
///              Dispatched by the Hangfire recurring job on the 1st of every month at 03:00 UTC.
/// </summary>
public sealed record SyncHazardsCommand(string CountryCode) : ICommand;
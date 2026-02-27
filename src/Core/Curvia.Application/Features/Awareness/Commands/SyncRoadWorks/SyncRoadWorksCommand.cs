using Templates.Core.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncRoadWorks;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Command that triggers a sync of road-work data for a given country.
///              Dispatched by the Hangfire recurring job every 4 hours per country code.
/// </summary>
public sealed record SyncRoadWorksCommand(string CountryCode) : ICommand;
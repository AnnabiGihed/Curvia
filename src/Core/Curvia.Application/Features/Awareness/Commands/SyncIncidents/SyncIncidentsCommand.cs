using Pivot.Framework.Application.Abstractions.Messaging.Commands;

namespace Curvia.Application.Features.Awareness.Commands.SyncIncidents;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Command that triggers a sync of live traffic incident data for a given country.
///              Dispatched by the Hangfire recurring job every 3 minutes per country code.
/// </summary>
public sealed record SyncIncidentsCommand(string CountryCode) : ICommand;
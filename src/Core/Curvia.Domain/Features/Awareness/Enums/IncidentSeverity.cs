namespace Curvia.Domain.Features.Awareness.Enums;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Severity of a live incident. Maps to DATEX II managementType / severity levels.
///              Used by mobile clients to decide alert prominence (sound vs banner vs silent).
/// </summary>
public enum IncidentSeverity
{
	Unknown = 0,
	Low = 1,
	Medium = 2,
	High = 3
}
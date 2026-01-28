namespace Curvia.Application.Features.Routing.Routes.Contracts;
public sealed record ValhallaTrip(ValhallaSummary Summary, IReadOnlyList<ValhallaLeg> Legs);
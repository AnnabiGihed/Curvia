using MediatR;
using Microsoft.AspNetCore.Mvc;
using Curvia.API.Middleware.Identity;
using Microsoft.AspNetCore.Authorization;
using Templates.Core.Containers.API.Abstractions;
using Curvia.Application.Features.Motorcycles.Responses;
using Curvia.Application.Features.Motorcycles.Commands.Add;
using Curvia.Application.Features.Motorcycles.Commands.Update;
using Curvia.Application.Features.Motorcycles.Commands.Remove;
using Curvia.Application.Features.Motorcycles.Queries.GetByUser;
using Curvia.Application.Features.Motorcycles.Commands.SetDefault;
using Curvia.API.DTOs.Motorcycles;

namespace Curvia.API.Controllers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : User garage (motorcycle) management endpoints.
///
///              All endpoints require authentication.
///              Ownership is enforced at the application layer — handlers use
///              FindByIdAndUserAsync so a user can never read or mutate another user's bikes.
///
///              Endpoints:
///                GET    /api/motorcycles              — list the authenticated user's garage
///                POST   /api/motorcycles              — add a motorcycle to the garage
///                PUT    /api/motorcycles/{id}         — update motorcycle details
///                DELETE /api/motorcycles/{id}         — remove a motorcycle (soft-delete)
///                PUT    /api/motorcycles/{id}/default — set a motorcycle as the route-gen default
/// </summary>
[Authorize]
[Route("api/motorcycles")]
public sealed class MotorcyclesController : ApiController
{
	#region Fields
	private readonly IAppUserContext _appUserContext;
	#endregion

	#region Constructor
	public MotorcyclesController(ISender sender, IAppUserContext appUserContext) : base(sender)
	{
		_appUserContext = appUserContext;
	}
	#endregion

	#region GET /api/motorcycles
	/// <summary>
	/// Returns all active motorcycles in the authenticated user's garage.
	/// Default bike is always first; remaining bikes are ordered by addition date descending.
	/// </summary>
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<MotorcycleDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetGarage(CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		return HandleResult(await Sender.Send(new GetUserMotorcyclesQuery(userId), ct));
	}
	#endregion

	#region POST /api/motorcycles
	/// <summary>
	/// Adds a new motorcycle to the authenticated user's garage.
	/// If <c>isDefault</c> is true, any previously set default is cleared automatically.
	/// </summary>
	[HttpPost]
	[ProducesResponseType(typeof(MotorcycleDto), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> AddMotorcycle([FromBody] AddMotorcycleRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var command = new AddMotorcycleCommand(	UserId: userId, Maker: request.Maker, Model: request.Model, Year: request.Year, EngineCc: request.EngineCc, Nickname: request.Nickname, IsDefault: request.IsDefault);
		var result = await Sender.Send(command, ct);

		if (result.IsFailure)
			return HandleFailure(result);

		return CreatedAtAction(nameof(GetGarage), null, result.Value);
	}
	#endregion

	#region PUT /api/motorcycles/{id}
	/// <summary>
	/// Updates the details (maker, model, year, engine cc, nickname) of an existing motorcycle.
	/// Only the owner may update their motorcycle.
	/// </summary>
	[HttpPut("{id:guid}")]
	[ProducesResponseType(typeof(MotorcycleDto), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UpdateMotorcycle([FromRoute] Guid id, [FromBody] UpdateMotorcycleRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var command = new UpdateMotorcycleCommand(UserId: userId, MotorcycleId: id, Maker: request.Maker, Model: request.Model, Year: request.Year, EngineCc: request.EngineCc, Nickname: request.Nickname);

		return HandleResult(await Sender.Send(command, ct));
	}
	#endregion

	#region DELETE /api/motorcycles/{id}
	/// <summary>
	/// Removes a motorcycle from the authenticated user's garage (soft-delete).
	/// Only the owner may remove their motorcycle.
	/// </summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> RemoveMotorcycle([FromRoute] Guid id, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new RemoveMotorcycleCommand(userId, id), ct);
		return result.IsFailure ? HandleFailure(result) : NoContent();
	}
	#endregion

	#region PUT /api/motorcycles/{id}/default
	/// <summary>
	/// Sets the specified motorcycle as the user's default — pre-selected on the route generation UI.
	/// Only one motorcycle may be the default at a time; the previous default is cleared automatically.
	/// </summary>
	[HttpPut("{id:guid}/default")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> SetDefault([FromRoute] Guid id, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new SetDefaultMotorcycleCommand(userId, id), ct);

		return result.IsFailure ? HandleFailure(result) : NoContent();
	}
	#endregion
}

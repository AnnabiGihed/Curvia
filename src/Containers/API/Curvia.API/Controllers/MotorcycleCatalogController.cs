using MediatR;
using Microsoft.AspNetCore.Mvc;
using Curvia.API.Middleware.Identity;
using Microsoft.AspNetCore.Authorization;
using Curvia.API.DTOs.MotorcycleCatalogs;
using Templates.Core.Containers.API.Abstractions;
using Curvia.Application.Features.MotorcycleCatalogs.Responses;
using Curvia.Application.Features.MotorcycleCatalogs.Commands.AddMaker;
using Curvia.Application.Features.MotorcycleCatalogs.Commands.AddModel;
using Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveMaker;
using Curvia.Application.Features.MotorcycleCatalogs.Commands.ApproveModel;
using Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestMaker;
using Curvia.Application.Features.MotorcycleCatalogs.Commands.SuggestModel;
using Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingMakers;
using Curvia.Application.Features.MotorcycleCatalogs.Queries.GetPendingModels;
using Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialMakers;
using Curvia.Application.Features.MotorcycleCatalogs.Queries.GetOfficialModels;

namespace Curvia.API.Controllers;

/// <summary>
/// Author      : Gihed Annabi
/// Date        : 02-2026
/// Purpose     : Motorcycle maker/model catalog endpoints.
/// </summary>
[Route("api/motorcycle-catalog")]
public sealed class MotorcycleCatalogController : ApiController
{
	#region Fields
	private readonly IAppUserContext _appUserContext;
	#endregion

	#region Constructor
	public MotorcycleCatalogController(ISender sender, IAppUserContext appUserContext)
		: base(sender)
	{
		_appUserContext = appUserContext;
	}
	#endregion

	#region Admin endpoints
	/// <summary>
	/// Lists all models awaiting admin approval.
	/// </summary>
	[HttpGet("admin/models/pending")]
	[Authorize(Roles = "Administrator")]
	[ProducesResponseType(typeof(IReadOnlyList<PendingModelDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetPendingModels(CancellationToken ct)
	{
		return HandleResult(await Sender.Send(new GetPendingModelsQuery(), ct));
	}

	/// <summary>
	/// Lists all makers awaiting admin approval.
	/// </summary>
	[HttpGet("admin/makers/pending")]
	[Authorize(Roles = "Administrator")]
	[ProducesResponseType(typeof(IReadOnlyList<PendingMakerDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetPendingMakers(CancellationToken ct)
	{
		return HandleResult(await Sender.Send(new GetPendingMakersQuery(), ct));
	}

	/// <summary>
	/// Admin approves a pending maker suggestion.
	/// </summary>
	[Authorize(Roles = "Administrator")]
	[HttpPut("admin/makers/{id:guid}/approve")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ApproveMaker([FromRoute] Guid id, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId) 
			return Unauthorized();

		var result = await Sender.Send(new ApproveMakerCommand(id, userId.ToString()), ct);
		return result.IsFailure ? HandleFailure(result) : NoContent();
	}

	/// <summary>
	/// Admin approves a pending model suggestion.
	/// </summary>
	[Authorize(Roles = "Administrator")]
	[HttpPut("admin/models/{id:guid}/approve")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ApproveModel([FromRoute] Guid id, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new ApproveModelCommand(id, userId.ToString()), ct);
		return result.IsFailure ? HandleFailure(result) : NoContent();
	}

	/// <summary>
	/// Admin adds a new official maker directly (no approval step).
	/// </summary>
	[HttpPost("admin/makers")]
	[Authorize(Roles = "Administrator")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[ProducesResponseType(typeof(MakerDto), StatusCodes.Status201Created)]
	public async Task<IActionResult> AddMaker([FromBody] AddMakerRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new AddMakerCommand(request.Name, userId.ToString()), ct);

		if (result.IsFailure) 
			return HandleFailure(result);

		return CreatedAtAction(nameof(GetMakers), null, result.Value);
	}

	/// <summary>
	/// Admin adds a new official model to an existing maker (no approval step).
	/// </summary>
	[Authorize(Roles = "Administrator")]
	[HttpPost("admin/makers/{makerId:guid}/models")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[ProducesResponseType(typeof(ModelDto), StatusCodes.Status201Created)]
	public async Task<IActionResult> AddModel([FromRoute] Guid makerId, [FromBody] AddModelRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new AddModelCommand(makerId, request.Name, request.Category, userId.ToString()), ct);

		if (result.IsFailure) 
			return HandleFailure(result);

		return CreatedAtAction(nameof(GetModels), new { makerId = result.Value.MakerId }, result.Value);
	}
	#endregion

	#region User endpoints
	/// <summary>
	/// All official motorcycle makers, sorted alphabetically.
	/// </summary>
	[AllowAnonymous]
	[HttpGet("makers")]
	[ProducesResponseType(typeof(IReadOnlyList<MakerDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetMakers(CancellationToken ct)
	{
		return HandleResult(await Sender.Send(new GetOfficialMakersQuery(), ct));
	}

	/// <summary>
	/// Official models for a given maker, sorted alphabetically.
	/// </summary>
	[AllowAnonymous]
	[HttpGet("makers/{makerId:guid}/models")]
	[ProducesResponseType(typeof(IReadOnlyList<ModelDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetModels([FromRoute] Guid makerId, CancellationToken ct)
	{
		return HandleResult(await Sender.Send(new GetOfficialModelsQuery(makerId), ct));
	}
	#endregion

	#region Authenticated user endpoints
	/// <summary>
	/// Suggest a maker not in the official list.
	/// The suggestion is stored as PendingValidation and shown to admins for review.
	/// </summary>
	[Authorize]
	[HttpPost("makers/suggest")]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[ProducesResponseType(typeof(MakerDto), StatusCodes.Status201Created)]
	public async Task<IActionResult> SuggestMaker([FromBody] SuggestMakerRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId) 
			return Unauthorized();

		var result = await Sender.Send(new SuggestMakerCommand(request.Name, userId), ct);

		if (result.IsFailure) 
			return HandleFailure(result);

		return CreatedAtAction(nameof(GetMakers), null, result.Value);
	}

	/// <summary>
	/// Suggest a model for a given maker.
	/// The suggestion is stored as PendingValidation and shown to admins for review.
	/// The maker must exist (either Official or itself PendingValidation).
	/// </summary>
	[Authorize]
	[HttpPost("makers/{makerId:guid}/models/suggest")]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[ProducesResponseType(typeof(ModelDto), StatusCodes.Status201Created)]
	public async Task<IActionResult> SuggestModel([FromRoute] Guid makerId, [FromBody] SuggestModelRequest request, CancellationToken ct)
	{
		if (_appUserContext.AppUserId is not { } userId)
			return Unauthorized();

		var result = await Sender.Send(new SuggestModelCommand(makerId, request.Name, request.Category, userId), ct);

		if (result.IsFailure) return HandleFailure(result);

		return CreatedAtAction(nameof(GetModels), new { makerId = result.Value.MakerId }, result.Value);
	}
	#endregion
}
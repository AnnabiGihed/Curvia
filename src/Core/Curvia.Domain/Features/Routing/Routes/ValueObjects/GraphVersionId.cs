using Templates.Core.Domain.Shared;
using Curvia.Domain.Features.Routing.Shared;

namespace Curvia.Domain.Features.Routing.Routes.ValueObjects;



/// <summary>
/// Author      : Gihed Annabi
/// Date        : 01-2026
/// Purpose     : Represents the version of the underlying routing graph dataset.
///              Typically an ETag/hash/semantic version.
///              Ensures non-empty and sane character set for transport/storage safety.
/// </summary>
public sealed class GraphVersionId : CSharpFunctionalExtensions.ValueObject<GraphVersionId>
{
	#region Constants

	// Keep this bounded to avoid abuse and keep indexes sane.
	private const int MaxLength = 128;

	#endregion

	#region Properties

	public string Value { get; }

	#endregion

	#region Constructor

	private GraphVersionId(string value)
	{
		Value = value;
	}

	#endregion

	#region Factory

	public static Result<GraphVersionId> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<GraphVersionId>(RoutingErrors.GraphVersionIdRequired());

		var trimmed = value.Trim();

		if (trimmed.Length > MaxLength)
			return Result.Failure<GraphVersionId>(RoutingErrors.GraphVersionIdTooLong(MaxLength));

		if (!HasAllowedChars(trimmed))
			return Result.Failure<GraphVersionId>(RoutingErrors.GraphVersionIdInvalidChars());

		return Result.Success(new GraphVersionId(trimmed));
	}

	private static bool HasAllowedChars(string input)
	{
		// Allowed: A-Z a-z 0-9 - _ . :
		for (var i = 0; i < input.Length; i++)
		{
			var c = input[i];

			var ok =
				(c >= 'a' && c <= 'z') ||
				(c >= 'A' && c <= 'Z') ||
				(c >= '0' && c <= '9') ||
				c == '-' || c == '_' || c == '.' || c == ':';

			if (!ok)
				return false;
		}

		return true;
	}

	#endregion

	#region Equality

	protected override bool EqualsCore(GraphVersionId other)
		=> string.Equals(Value, other.Value, StringComparison.Ordinal);

	protected override int GetHashCodeCore()
		=> StringComparer.Ordinal.GetHashCode(Value);

	#endregion

	#region Overrides

	public override string ToString() => Value;

	#endregion
}

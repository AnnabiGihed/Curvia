using System.Reflection;

namespace Curvia.Domain;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
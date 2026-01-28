using System.Reflection;

namespace Curvia.Infrastructure;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
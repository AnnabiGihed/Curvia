using System.Reflection;

namespace Curvia.Persistence.EntityFrameworkCore;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
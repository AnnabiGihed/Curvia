using System.Reflection;

namespace Curvia.Web.App;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
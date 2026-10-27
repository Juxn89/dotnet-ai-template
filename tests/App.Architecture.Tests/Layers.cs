namespace App.Architecture.Tests;

using System.Reflection;
using App.Application.Common;
using App.Domain.Notes;
using App.Infrastructure;

/// <summary>
/// Assembly OBJECTS, not names: ArchUnitNET's ResideInAssembly(string) matches the full assembly name, so a
/// simple name matches nothing and every rule passes vacuously. Learned in talent-mcp.
/// </summary>
internal static class Layers
{
	public static readonly Assembly Domain = typeof(Note).Assembly;

	public static readonly Assembly Application = typeof(Error).Assembly;

	public static readonly Assembly Infrastructure = typeof(DependencyInjection).Assembly;

	public static readonly Assembly Api = typeof(Program).Assembly;
}

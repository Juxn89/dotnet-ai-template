namespace App.Infrastructure.Ai;

using System.Security.Cryptography;
using System.Text;

/// <summary>Prompts are Markdown files in <c>Ai/Prompts</c>, embedded in the assembly and versioned by their hash.</summary>
internal static class PromptLibrary
{
	private const int HashLength = 12;

	/// <summary>Reads one prompt.</summary>
	/// <param name="name">The file name under <c>Ai/Prompts</c>.</param>
	/// <returns>Its text and its version.</returns>
	public static (string Text, string Hash) Read(string name)
	{
		var assembly = typeof(PromptLibrary).Assembly;
		var resource = assembly.GetManifestResourceNames().Single(r => r.EndsWith("." + name, StringComparison.Ordinal));
		using var stream = assembly.GetManifestResourceStream(resource)!;
		using var reader = new StreamReader(stream);
		var text = reader.ReadToEnd().Trim();
		return (text, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..HashLength]);
	}
}

// Turns this template into a project: renames "App" (projects, namespaces, the solution, Aspire's generated names)
// and "dotnet-ai-template" (repo URLs, volume, package and ledger names) in every text file and path.
//
//   node scripts/rename.mjs <PascalName> <kebab-slug>     e.g.  node scripts/rename.mjs BookingAgent booking-agent
//
// Node only, no dependencies. Run it once, on a fresh copy made with GitHub's "Use this template"; then follow the
// steps it prints. It refuses to run twice: after the first run there is no App.slnx left.
import { existsSync, readdirSync, readFileSync, renameSync, statSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const self = fileURLToPath(import.meta.url);
const root = path.resolve(path.dirname(self), "..");
const [name, slug] = process.argv.slice(2);

if (!name || !/^[A-Z][A-Za-z0-9]+$/.test(name) || !slug || !/^[a-z][a-z0-9-]+$/.test(slug)) {
	console.error("Usage: node scripts/rename.mjs <PascalName> <kebab-slug>   e.g. BookingAgent booking-agent");
	process.exit(2);
}

if (!existsSync(path.join(root, "App.slnx"))) {
	console.error("App.slnx not found: this copy was already renamed.");
	process.exit(2);
}

// Folders never touched: build output, dependencies, git, and binary media.
const skipped = new Set([".git", "node_modules", "bin", "obj", ".next", "test-results", "playwright-report", "results"]);
const textExtensions = new Set([".cs", ".csproj", ".props", ".slnx", ".json", ".md", ".yml", ".yaml", ".ts", ".tsx", ".mjs", ".sh", ".css", ".template", ".editorconfig", ".gitignore"]);

// "App." only before this template's own project names, so "App" in prose or in other identifiers stays.
const layers = ["Domain", "Application", "Infrastructure", "Api", "AppHost", "ServiceDefaults", "Evals", "Architecture", "Integration", "Ai"];
const projectPrefix = new RegExp(`\\bApp\\.(?=(?:${layers.join("|")})\\b)`, "g");
const replacements = [
	[projectPrefix, `${name}.`],
	[/\bProjects\.App_/g, `Projects.${name}_`],
	[/\bApp\.slnx\b/g, `${name}.slnx`],
	[/dotnet-ai-template/g, slug],
];

function walk(directory) {
	return readdirSync(directory).flatMap((entry) => {
		if (skipped.has(entry)) {
			return [];
		}
		const full = path.join(directory, entry);
		return statSync(full).isDirectory() ? [...walk(full), full] : [full];
	});
}

const paths = walk(root);
let edited = 0;
// Not this script: it names what it replaces, and would no longer know a renamed copy from a fresh one.
for (const file of paths.filter((p) => p !== self && statSync(p).isFile() && (textExtensions.has(path.extname(p)) || textExtensions.has(path.basename(p))))) {
	const before = readFileSync(file, "utf8");
	const after = replacements.reduce((text, [pattern, value]) => text.replace(pattern, value), before);
	if (after !== before) {
		writeFileSync(file, after);
		edited++;
	}
}

// Deepest paths first, so a folder is renamed after the files inside it.
let renamed = 0;
for (const full of paths.sort((a, b) => b.length - a.length)) {
	const base = path.basename(full);
	if (base.startsWith("App.")) {
		renameSync(full, path.join(path.dirname(full), name + base.slice("App".length)));
		renamed++;
	}
}

console.log(`${edited} files edited, ${renamed} paths renamed: App → ${name}, dotnet-ai-template → ${slug}.

Next:
  1. Re-verify every version in Directory.Packages.props and web/package.json (two weeks old, OSI licence).
  2. Replace the Notes example with your first slice (src/*/Notes, the endpoints, the tests, web/components,
     evals/notes.json, web/e2e) and add a migration: dotnet ef migrations add InitialSchema.
  3. Set the ledger session's cap in PaidRun.cs and AGENTS.md, and the README.
  4. dotnet build && dotnet test && dotnet run --project evals/${name}.Evals -- oracle`);

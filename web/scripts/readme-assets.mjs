// Generates the README artwork in docs/readme/: hero banner, metrics strip, feature icons and divider (SVG,
// light and dark), and the showcase (PNG), composed from two screenshots `pnpm demo` writes to docs/demo/
// (a note with its summary, tags and cost, and the same page in English with the day's spend).
//
//   node scripts/readme-assets.mjs
//
// GitHub shows README images through <img>: plain SVG, no external fonts or scripts. CSS animations inside the
// SVG do run there, and they stop under prefers-reduced-motion.
import { chromium } from "@playwright/test";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";

const out = fileURLToPath(new URL("../../docs/readme/", import.meta.url));
const demo = fileURLToPath(new URL("../../docs/demo/", import.meta.url));
await mkdir(out, { recursive: true });

// Its own brand, distinct from docs-assistant (teal), claude-pr-review-bot (violet), invoice-extractor (blue),
// booking-agent (indigo) and data-chat (rose); amber is the shared accent.
const brand = { slate: "#334155", lime: "#84cc16", limeDark: "#4d7c0f", amber: "#f59e0b", green: "#16a34a" };

const themes = {
	dark: { bg: "#0a0d12", panel: "#121720", card: "#161c27", border: "#283244", text: "#f1f5f9", muted: "#94a3b8", line: "#2b3546", dot: "#ffffff", dotOpacity: 0.06, orb: 0.3, accent: brand.lime },
	light: { bg: "#f7f9f4", panel: "#ffffff", card: "#ffffff", border: "#e1e7da", text: "#0f172a", muted: "#475569", line: "#dde5d2", dot: "#334155", dotOpacity: 0.07, orb: 0.14, accent: brand.limeDark },
};

const sans = `'Segoe UI', Inter, -apple-system, BlinkMacSystemFont, 'Helvetica Neue', Arial, sans-serif`;
const mono = `'Cascadia Code', 'SFMono-Regular', Consolas, Menlo, monospace`;
const escape = (text) => text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

function defs(t) {
	return `
	<linearGradient id="brand" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="${brand.lime}"/><stop offset=".6" stop-color="${brand.limeDark}"/><stop offset="1" stop-color="${brand.amber}"/></linearGradient>
	<linearGradient id="metric" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${brand.lime}"/><stop offset=".55" stop-color="${brand.limeDark}"/><stop offset="1" stop-color="${brand.amber}"/></linearGradient>
	<radialGradient id="orbMain"><stop offset="0" stop-color="${brand.lime}" stop-opacity="${t.orb}"/><stop offset="1" stop-color="${brand.lime}" stop-opacity="0"/></radialGradient>
	<radialGradient id="orbAmber"><stop offset="0" stop-color="${brand.amber}" stop-opacity="${t.orb * 0.7}"/><stop offset="1" stop-color="${brand.amber}" stop-opacity="0"/></radialGradient>
	<pattern id="dots" width="22" height="22" patternUnits="userSpaceOnUse"><circle cx="2" cy="2" r="1.1" fill="${t.dot}" fill-opacity="${t.dotOpacity}"/></pattern>`;
}

// ── Hero: what it does, drawn — a note goes through a metered, capped call and comes back with a checked summary ──
function hero(name) {
	const t = themes[name];
	const W = 1280;
	const H = 460;
	const chips = ["Claude Haiku 5.5", "Metered & capped", "Evals in CI", "E2E without a key", "rename.mjs"];
	let x = 72;
	const chipSvg = chips
		.map((chip) => {
			const w = chip.length * 8.4 + 30;
			const g = `<g transform="translate(${x} 352)"><rect width="${w}" height="32" rx="16" fill="${t.panel}" stroke="${t.border}"/><text x="${w / 2}" y="21" text-anchor="middle" font-family="${sans}" font-size="14" font-weight="600" fill="${t.text}">${escape(chip)}</text></g>`;
			x += w + 8;
			return g;
		})
		.join("");

	const tags = ["shopping", "money"]
		.map((tag, i) => `<g transform="translate(${16 + i * 84} 128)"><g class="pop" style="animation-delay:${1.2 + i * 0.15}s"><rect width="76" height="24" rx="12" fill="${brand.slate}"/><text x="38" y="16.5" text-anchor="middle" font-family="${sans}" font-size="12" font-weight="700" fill="#fff">${tag}</text></g></g>`)
		.join("");

	return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" role="img" aria-labelledby="title desc">
<title id="title">dotnet-ai-template</title>
<desc id="desc">A .NET 10 template for AI products: layered slices, Claude behind a port, every call metered and capped, evals with an oracle in CI.</desc>
<style>
	.flow { stroke-dasharray: 6 7; animation: flow 1.6s linear infinite; }
	.pop { opacity: 0; animation: pop .5s ease-out forwards; }
	.glow { animation: glow 2.4s ease-in-out infinite alternate; }
	@keyframes flow { to { stroke-dashoffset: -26; } }
	@keyframes pop { from { opacity: 0; transform: translateY(6px); } to { opacity: 1; transform: none; } }
	@keyframes glow { from { opacity: .55; } to { opacity: 1; } }
	@media (prefers-reduced-motion: reduce) { .flow, .glow { animation: none; } .pop { animation: none; opacity: 1; } }
</style>
<defs>${defs(t)}
	<clipPath id="frame"><rect width="${W}" height="${H}" rx="28"/></clipPath>
	<filter id="soft" x="-20%" y="-20%" width="140%" height="160%"><feDropShadow dx="0" dy="14" stdDeviation="16" flood-color="#000" flood-opacity="${name === "dark" ? 0.45 : 0.12}"/></filter>
</defs>
<g clip-path="url(#frame)">
	<rect width="${W}" height="${H}" fill="${t.bg}"/>
	<rect width="${W}" height="${H}" fill="url(#dots)"/>
	<circle cx="180" cy="40" r="340" fill="url(#orbMain)"/>
	<circle cx="1060" cy="470" r="360" fill="url(#orbAmber)"/>
</g>
<rect x=".5" y=".5" width="${W - 1}" height="${H - 1}" rx="28" fill="none" stroke="${t.border}"/>

<g transform="translate(72 70)">
	<rect width="290" height="30" rx="15" fill="${t.panel}" stroke="${t.border}"/>
	<circle cx="18" cy="15" r="4.5" fill="${brand.lime}" class="glow"/>
	<text x="31" y="20" font-family="${sans}" font-size="12.5" font-weight="700" letter-spacing=".1em" fill="${t.muted}">GITHUB TEMPLATE · .NET 10 · MIT</text>
</g>
<text x="68" y="168" font-family="${sans}" font-size="56" font-weight="800" letter-spacing="-.03em" fill="${t.text}">dotnet-<tspan fill="url(#brand)">ai-template</tspan></text>
<text x="72" y="226" font-family="${sans}" font-size="24" font-weight="600" fill="${t.text}">Start an AI product on day 1, not day 10.</text>
<text x="72" y="264" font-family="${sans}" font-size="20" fill="${t.muted}">Layers, a model behind a port, costs and caps, evals, E2E.</text>
<text x="72" y="296" font-family="${sans}" font-size="20" fill="${t.muted}">Extracted from four shipped projects, not designed in a vacuum.</text>
${chipSvg}

<!-- The note -->
<g transform="translate(668 62)" filter="url(#soft)">
	<rect width="232" height="80" rx="16" fill="${t.card}" stroke="${t.border}"/>
	<text x="16" y="28" font-family="${sans}" font-size="13.5" font-weight="700" fill="${t.text}">Pedido de café</text>
	<text x="16" y="49" font-family="${sans}" font-size="12" fill="${t.muted}">Llamar al proveedor el lunes,</text>
	<text x="16" y="66" font-family="${sans}" font-size="12" fill="${t.muted}">10 kilos, confirmar la factura…</text>
</g>

<path d="M784 144 C 784 168, 790 172, 790 188" fill="none" stroke="${t.accent}" stroke-width="2.2" class="flow"/>

<!-- The metered call -->
<g transform="translate(668 190)" filter="url(#soft)">
	<rect width="232" height="132" rx="18" fill="${t.card}" stroke="${t.border}"/>
	<text x="18" y="30" font-family="${sans}" font-size="11.5" font-weight="700" letter-spacing=".08em" fill="${t.accent}">METERED · CAPPED</text>
	<text x="18" y="58" font-family="${mono}" font-size="12.5" fill="${t.text}">spent today  0.0002</text>
	<text x="18" y="80" font-family="${mono}" font-size="12.5" fill="${t.text}">daily budget 1.00</text>
	<g transform="translate(18 96)"><g class="pop" style="animation-delay:.5s">
		<rect width="140" height="22" rx="11" fill="${brand.green}" fill-opacity=".14" stroke="${brand.green}"/>
		<text x="70" y="15" text-anchor="middle" font-family="${sans}" font-size="11.5" font-weight="700" fill="${brand.green}">under the cap ✓</text>
	</g></g>
</g>

<path d="M900 256 C 944 256, 944 186, 990 182" fill="none" stroke="${brand.amber}" stroke-width="2.2" class="flow"/>

<!-- The answer, checked by the domain -->
<g transform="translate(990 92)" filter="url(#soft)">
	<rect width="224" height="214" rx="18" fill="${t.card}" stroke="${t.border}"/>
	<text x="16" y="30" font-family="${sans}" font-size="13" font-weight="700" fill="${t.text}">Summary</text>
	<circle cx="204" cy="25" r="4.5" fill="${brand.amber}" class="glow"/>
	<text x="16" y="56" font-family="${sans}" font-size="12" fill="${t.muted}">Call the coffee supplier on</text>
	<text x="16" y="74" font-family="${sans}" font-size="12" fill="${t.muted}">Monday for 10 kg and the</text>
	<text x="16" y="92" font-family="${sans}" font-size="12" fill="${t.muted}">invoice.</text>
	${tags}
	<text x="16" y="178" font-family="${mono}" font-size="10.5" fill="${t.muted}">tags from the domain's list</text>
	<text x="16" y="196" font-family="${mono}" font-size="10.5" fill="${t.muted}">USD 0.000099 · ai_usage</text>
</g>
</svg>
`;
}

// ── Metrics: measured by this repo's evals, tests and demo (evals/results, CI, 27 Oct 2026) ───────────────────
const metricItems = [
	["10 / 10", "eval notes tagged right", "Claude Haiku 5.5, 27 Oct 2026"],
	["USD 0.0001", "per summary", "metered on every call"],
	["36", "tests, end to end included", "no key and no cost in CI"],
	["4", "projects it was extracted from", "what they repeated, nothing more"],
];

function metrics(name) {
	const t = themes[name];
	const W = 1280;
	const H = 150;
	const col = W / metricItems.length;
	const cells = metricItems
		.map(
			([value, label, note], i) => `
	<g transform="translate(${col * i + col / 2} 0)">
		<text y="64" text-anchor="middle" font-family="${sans}" font-size="40" font-weight="800" letter-spacing="-.02em" fill="url(#metric)">${escape(value)}</text>
		<text y="94" text-anchor="middle" font-family="${sans}" font-size="16" font-weight="600" fill="${t.text}">${escape(label)}</text>
		<text y="116" text-anchor="middle" font-family="${sans}" font-size="13.5" fill="${t.muted}">${escape(note)}</text>
	</g>${i > 0 ? `<line x1="${col * i}" y1="36" x2="${col * i}" y2="114" stroke="${t.border}"/>` : ""}`,
		)
		.join("");
	const label = metricItems.map(([value, label, note]) => `${value} ${label}, ${note}.`).join(" ");
	return `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" role="img" aria-label="${escape(label)}">
<defs>${defs(t)}</defs>
<rect x=".5" y=".5" width="${W - 1}" height="${H - 1}" rx="22" fill="${t.panel}" stroke="${t.border}"/>
${cells}
</svg>
`;
}

// ── Feature icons: Lucide glyphs (ISC license) on a gradient tile ─────────────────────────────────────────
const glyphs = {
	layers: '<path d="M12.83 2.18a2 2 0 0 0-1.66 0L2.6 6.08a1 1 0 0 0 0 1.83l8.58 3.91a2 2 0 0 0 1.66 0l8.58-3.9a1 1 0 0 0 0-1.83z"/><path d="m22 17.65-9.17 4.16a2 2 0 0 1-1.66 0L2 17.65"/><path d="m22 12.65-9.17 4.16a2 2 0 0 1-1.66 0L2 12.65"/>',
	chat: '<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/><path d="M8 9h8"/><path d="M8 13h5"/>',
	gauge: '<path d="m12 14 4-4"/><path d="M3.34 19a10 10 0 1 1 17.32 0"/>',
	check: '<path d="M3.85 8.62a4 4 0 0 1 4.78-4.77 4 4 0 0 1 6.74 0 4 4 0 0 1 4.78 4.78 4 4 0 0 1 0 6.74 4 4 0 0 1-4.77 4.78 4 4 0 0 1-6.75 0 4 4 0 0 1-4.78-4.77 4 4 0 0 1 0-6.76z"/><path d="m9 12 2 2 4-4"/>',
	evals: '<path d="M14 2v6a2 2 0 0 0 .245.96l5.51 10.08A2 2 0 0 1 18 22H6a2 2 0 0 1-1.755-2.96l5.51-10.08A2 2 0 0 0 10 8V2"/><path d="M6.453 15h11.094"/><path d="M8.5 2h7"/>',
	play: '<rect width="20" height="14" x="2" y="3" rx="2"/><path d="M8 21h8"/><path d="M12 17v4"/><path d="m10 7 4 3-4 3z"/>',
	shield: '<path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"/><path d="m9 12 2 2 4-4"/>',
	code: '<polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>',
	copy: '<rect width="14" height="14" x="8" y="8" rx="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>',
};

const tiles = {
	layers: ["#334155", "#64748b"],
	chat: ["#4d7c0f", "#a3e635"],
	gauge: ["#b45309", "#fbbf24"],
	check: ["#0e7490", "#22d3ee"],
	evals: ["#5b21b6", "#a78bfa"],
	play: ["#1e3a8a", "#60a5fa"],
	shield: ["#166534", "#4ade80"],
	code: ["#7c2d12", "#f97316"],
	copy: ["#9f1239", "#fb7185"],
};

function iconSvg(name) {
	const [from, to] = tiles[name];
	return `<svg xmlns="http://www.w3.org/2000/svg" width="56" height="56" viewBox="0 0 56 56" aria-hidden="true">
<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${from}"/><stop offset="1" stop-color="${to}"/></linearGradient></defs>
<rect width="56" height="56" rx="16" fill="url(#g)"/>
<rect x=".75" y=".75" width="54.5" height="54.5" rx="15.25" fill="none" stroke="#fff" stroke-opacity=".18" stroke-width="1.5"/>
<g transform="translate(16 16)" fill="none" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${glyphs[name]}</g>
</svg>
`;
}

const divider = `<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="24" viewBox="0 0 1280 24" aria-hidden="true">
<defs><linearGradient id="d" x1="0" x2="1"><stop offset="0" stop-color="${brand.lime}" stop-opacity="0"/><stop offset=".3" stop-color="${brand.limeDark}"/><stop offset=".7" stop-color="${brand.amber}"/><stop offset="1" stop-color="${brand.amber}" stop-opacity="0"/></linearGradient></defs>
<rect y="11" width="1280" height="2" rx="1" fill="url(#d)"/>
<rect x="632" y="4" width="16" height="16" rx="5" fill="${brand.slate}"/><path d="M636.5 12l2.5 2.5 4.5-5" fill="none" stroke="#fff" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
`;

for (const name of ["dark", "light"]) {
	await writeFile(`${out}hero-${name}.svg`, hero(name));
	await writeFile(`${out}metrics-${name}.svg`, metrics(name));
}

for (const name of Object.keys(glyphs)) {
	await writeFile(`${out}icon-${name}.svg`, iconSvg(name));
}

await writeFile(`${out}divider.svg`, divider);

// ── Showcase: the real app, two browser windows on each theme's background ────────────────────────────────
const shot = async (file) => `data:image/png;base64,${(await readFile(`${demo}${file}`)).toString("base64")}`;
const spanish = await shot("01-resumen.png");
const english = await shot("03-ingles.png");
const browser = await chromium.launch();

for (const name of ["dark", "light"]) {
	const t = themes[name];
	const page = await browser.newPage({ viewport: { width: 1600, height: 1000 }, deviceScaleFactor: 1.5 });
	const window = (left, top, url, image, z) => `
		<div class="window" style="left:${left}px;top:${top}px;z-index:${z}"><div class="bar"><span class="d" style="background:#f87171"></span><span class="d" style="background:#fbbf24"></span><span class="d" style="background:#34d399"></span>
			<span class="url">${url}</span></div><div class="view"><img src="${image}"></div></div>`;
	await page.setContent(
		`<!doctype html><html><head><meta charset="utf-8"><style>
		* { box-sizing: border-box; margin: 0; } body { width: 100vw; height: 100vh; overflow: hidden; position: relative; background: ${t.bg}; font-family: ${sans}; }
		.dots { position: absolute; inset: 0; background-image: radial-gradient(${t.dot} 1px, transparent 1.2px); background-size: 22px 22px; opacity: ${t.dotOpacity * 1.4}; }
		.orb { position: absolute; border-radius: 50%; filter: blur(100px); }
		.o1 { width: 700px; height: 700px; left: -200px; top: -260px; background: ${brand.lime}; opacity: ${name === "dark" ? 0.3 : 0.16}; }
		.o2 { width: 700px; height: 700px; right: -220px; bottom: -300px; background: ${brand.amber}; opacity: ${name === "dark" ? 0.25 : 0.16}; }
		.window { position: absolute; width: 900px; border-radius: 16px; overflow: hidden; background: ${t.panel}; border: 1px solid ${t.border};
			box-shadow: 0 50px 100px -20px rgba(0,0,0,${name === "dark" ? 0.65 : 0.25}); }
		.bar { height: 40px; display: flex; align-items: center; gap: 8px; padding: 0 14px; border-bottom: 1px solid ${t.border}; }
		.d { width: 11px; height: 11px; border-radius: 50%; }
		.url { margin: 0 auto; width: 340px; height: 26px; border-radius: 8px; background: ${t.bg}; border: 1px solid ${t.border}; color: ${t.muted};
			font-size: 12.5px; display: flex; align-items: center; justify-content: center; }
		.view { overflow: hidden; } .view img { display: block; width: 100%; }
		</style></head><body><div class="dots"></div><div class="orb o1"></div><div class="orb o2"></div>
		${window(60, 70, "localhost:3040", spanish, 1)}${window(640, 380, "localhost:3040 · English", english, 2)}</body></html>`,
		{ waitUntil: "load" },
	);
	await page.screenshot({ path: `${out}showcase-${name}.png` });
	await page.close();
}

await browser.close();
console.log(`README artwork written to ${out}`);

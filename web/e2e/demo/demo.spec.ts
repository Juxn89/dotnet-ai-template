import { expect, test, type Locator, type Page } from "@playwright/test";
import { execFileSync } from "node:child_process";
import { mkdir, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { baseURL } from "../servers";

// The walkthrough for the README and the portfolio, recorded against the real model (playwright.demo.config.ts):
// two notes, their summaries and tags with what each call cost, and the day's spend against the cap. It writes the
// video, its poster and the screenshots to docs/demo.
//
// Frames come from Chromium's screencast (1280×720 at device scale 1.5), with their timestamps; ffmpeg, in a container
// (pinned image, no network), turns them into a 1920×1080 MP4, so nothing is installed.

const out = path.resolve(__dirname, "../../../docs/demo");
const ffmpegImage = "jrottenberg/ffmpeg:9.0.1-ubuntu2404@sha256:7e8154ec4779d335054de5e173f1ef52032abaa12ac7416cc7f0c88f4973bd14";
const viewport = { width: 1280, height: 720 };
const scale = 1.5;
const fps = 30;

const notes = [
	{
		title: "Pedido de café",
		body: "Llamar al proveedor de café el lunes para pedir 10 kilos y confirmar el precio de la factura.",
	},
	{
		title: "Newsletter idea",
		body: "Start a monthly newsletter with recipes that use our coffee, to bring back customers.",
	},
];

declare global {
	interface Window {
		demoCaption?: (text: string) => void;
	}
}

// A pointer that follows the mouse and a caption at the bottom.
async function addOverlay(page: Page) {
	await page.addInitScript(() => {
		const ensure = (id: string, style: string) => {
			let element = document.getElementById(id);
			if (!element) {
				element = document.createElement("div");
				element.id = id;
				element.setAttribute("style", style);
				document.body.appendChild(element);
			}
			return element;
		};
		const pointer = () =>
			ensure(
				"demo-pointer",
				"position:fixed;z-index:2147483647;width:22px;height:22px;margin:-11px 0 0 -11px;border-radius:50%;background:rgba(77,124,15,.25);border:2px solid #4d7c0f;pointer-events:none;transition:transform .12s",
			);
		window.addEventListener("mousemove", (event) => {
			pointer().style.left = `${event.clientX}px`;
			pointer().style.top = `${event.clientY}px`;
		});
		window.addEventListener("mousedown", () => (pointer().style.transform = "scale(.6)"));
		window.addEventListener("mouseup", () => (pointer().style.transform = "scale(1)"));
		window.demoCaption = (text: string) => {
			const caption = ensure(
				"demo-caption",
				"position:fixed;z-index:2147483646;left:50%;bottom:22px;transform:translateX(-50%);width:max-content;max-width:min(980px,92vw);padding:12px 22px;border-radius:12px;background:rgba(15,23,42,.93);color:#fff;font:500 19px/1.45 system-ui,sans-serif;text-align:center;box-shadow:0 12px 32px rgba(0,0,0,.28);pointer-events:none",
			);
			caption.textContent = text;
			caption.style.display = text ? "block" : "none";
		};
	});
}

async function say(page: Page, text: string, holdMs: number) {
	await page.evaluate((caption) => window.demoCaption?.(caption), text);
	await page.waitForTimeout(holdMs);
}

// Screenshots without the overlay: they stand on their own.
async function shot(page: Page, file: string) {
	const hidden = ["demo-caption", "demo-pointer"];
	await page.evaluate((ids) => ids.forEach((id) => document.getElementById(id)?.style.setProperty("visibility", "hidden")), hidden);
	await page.screenshot({ path: file });
	await page.evaluate((ids) => ids.forEach((id) => document.getElementById(id)?.style.removeProperty("visibility")), hidden);
}

// One JPEG per composited frame, with its timestamp: the screencast only sends what changed.
async function record(page: Page, dir: string) {
	const frames: { file: string; at: number }[] = [];
	const cdp = await page.context().newCDPSession(page);
	cdp.on("Page.screencastFrame", async ({ data, metadata, sessionId }) => {
		const file = `f${String(frames.length).padStart(5, "0")}.jpg`;
		frames.push({ file, at: metadata.timestamp ?? Date.now() / 1000 });
		await writeFile(path.join(dir, file), Buffer.from(data, "base64"));
		await cdp.send("Page.screencastFrameAck", { sessionId }).catch(() => undefined);
	});
	const size = { maxWidth: viewport.width * scale, maxHeight: viewport.height * scale };
	await cdp.send("Page.startScreencast", { format: "jpeg", quality: 92, ...size, everyNthFrame: 1 });
	return async () => {
		await cdp.send("Page.stopScreencast");
		return frames;
	};
}

// A concat list that covers [start, end]: the first frame from the start, each frame until the next one.
function concatList(frames: { file: string; at: number }[], start: number, end: number) {
	const lines = frames.map((frame, i) => {
		const from = i === 0 ? start : frame.at;
		return `file '${frame.file}'\nduration ${((frames[i + 1]?.at ?? end) - from).toFixed(4)}`;
	});
	return `${lines.join("\n")}\nfile '${frames.at(-1)?.file}'\n`;
}

// Writes a note the way a person does and waits for its card.
async function write(page: Page, title: string, body: string): Promise<Locator> {
	await page.getByLabel("Título").click();
	await page.getByLabel("Título").pressSequentially(title, { delay: 45 });
	await page.getByLabel("Texto").click();
	await page.getByLabel("Texto").pressSequentially(body, { delay: 18 });
	await page.getByRole("button", { name: "Guardar nota" }).click();
	const card = page.getByRole("article", { name: title });
	await expect(card).toBeVisible();
	return card;
}

async function summarize(card: Locator) {
	await card.getByRole("button", { name: "Resumir con IA" }).click();
	await expect(card.getByText("Costo de IA:")).toBeVisible();
	await card.scrollIntoViewIfNeeded();
}

test("demo", async ({ browser }) => {
	await mkdir(out, { recursive: true });
	const frames = test.info().outputPath("frames");
	await rm(frames, { recursive: true, force: true });
	await mkdir(frames, { recursive: true });

	const page = await (await browser.newContext({ viewport, deviceScaleFactor: scale, locale: "es-ES", baseURL })).newPage();
	await addOverlay(page);
	await page.goto("/");
	await page.waitForLoadState("networkidle");
	await page.mouse.move(640, 400);

	const stop = await record(page, frames);
	const started = Date.now() / 1000;

	await say(page, "El ejemplo del template: notas con un resumen de IA. Se borra al crear un proyecto; la base queda.", 5000);

	const coffee = await write(page, notes[0]!.title, notes[0]!.body);
	await say(page, "Claude Haiku propone una frase y etiquetas de una lista fija; el dominio vuelve a validarlas.", 0);
	await summarize(coffee);
	// The poster is this moment, taken from the video: the summary, its tags and its cost.
	const posterAt = Date.now() / 1000 + 1.5;
	await page.waitForTimeout(5000);
	await shot(page, path.join(out, "01-resumen.png"));

	await say(page, "En el idioma de la nota: la IA nombra el idioma antes de escribir.", 0);
	const newsletter = await write(page, notes[1]!.title, notes[1]!.body);
	await summarize(newsletter);
	await page.waitForTimeout(4500);

	await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" }));
	await say(page, "Cada llamada queda medida: tokens, costo y un tope diario que se revisa antes de llamar.", 5500);
	await shot(page, path.join(out, "02-gasto.png"));

	await page.getByRole("button", { name: "en", exact: true }).click();
	await say(page, "La interfaz en español e inglés. CI con E2E, evals con oráculo y arquitectura verificada.", 4500);
	await shot(page, path.join(out, "03-ingles.png"));

	await say(page, "dotnet-ai-template · .NET 10 · Claude Haiku · github.com/Juxn89/dotnet-ai-template", 4500);
	const ended = Date.now() / 1000;
	const recorded = await stop();

	const start = recorded[0]?.at ?? started;
	await writeFile(path.join(frames, "frames.txt"), concatList(recorded, start, ended));
	execFileSync(
		"docker",
		[
			"run", "--rm", "--network", "none", "--read-only", "--tmpfs", "/tmp",
			"-v", `${frames}:/in`, "-v", `${out}:/out`, ffmpegImage,
			"-y", "-loglevel", "error",
			"-f", "concat", "-safe", "0", "-i", "/in/frames.txt",
			"-vf", `fps=${fps},scale=1920:1080:flags=lanczos,setsar=1,format=yuv420p`,
			"-c:v", "libx264", "-preset", "slow", "-crf", "20", "-movflags", "+faststart",
			"/out/demo.mp4",
		],
		{ stdio: "inherit" },
	);
	execFileSync(
		"docker",
		[
			"run", "--rm", "--network", "none", "--read-only", "--tmpfs", "/tmp", "-v", `${out}:/out`, ffmpegImage,
			"-y", "-loglevel", "error", "-ss", (posterAt - start).toFixed(2), "-i", "/out/demo.mp4", "-frames:v", "1", "-q:v", "3", "/out/poster.jpg",
		],
		{ stdio: "inherit" },
	);
	console.log(`${recorded.length} frames, ${(ended - start).toFixed(1)} s → ${path.join(out, "demo.mp4")}`);
});

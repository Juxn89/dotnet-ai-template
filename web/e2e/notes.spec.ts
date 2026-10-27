import { expect, test } from "@playwright/test";

// The template's example in one go: write a note, ask for its summary, see the tags, the cost and today's spend;
// a reply the domain rejects shows an error and saves nothing. The model plays a script; everything else is real.
test("a note gets an AI summary with tags and its cost, and today's spend counts the call", async ({ page }) => {
	await page.goto("/");
	await expect(page.getByRole("heading", { name: "Tus notas" })).toBeVisible();
	await expect(page.getByText("Todavía no hay notas")).toBeVisible();

	await page.getByLabel("Título").fill("Pedido de café");
	await page.getByLabel("Texto").fill("Llamar al proveedor de café el lunes para pedir 10 kilos.");
	await page.getByRole("button", { name: "Guardar nota" }).click();

	const card = page.getByRole("article", { name: "Pedido de café" });
	await expect(card).toBeVisible();
	await card.getByRole("button", { name: "Resumir con IA" }).click();

	await expect(card.getByText("Llamar al proveedor de café el lunes para pedir 10 kilos.", { exact: false }).last()).toBeVisible();
	await expect(card.getByRole("list", { name: "Etiquetas" }).getByRole("listitem")).toHaveText(["trabajo", "compras"]);
	await expect(card.getByText("Costo de IA: USD 0.00")).toBeVisible();
	await expect(page.getByText(/· 1 llamada$/)).toBeVisible();

	// A reply off the domain's tag list is the model's failure: an error, and no summary saved.
	await page.getByLabel("Título").fill("Prueba de reglas");
	await page.getByLabel("Texto").fill("Una nota sin etiquetas válidas.");
	await page.getByRole("button", { name: "Guardar nota" }).click();
	const rejected = page.getByRole("article", { name: "Prueba de reglas" });
	await rejected.getByRole("button", { name: "Resumir con IA" }).click();
	await expect(rejected.getByRole("alert")).toHaveText("La IA no pudo resumir la nota. Inténtalo de nuevo.");
	await expect(rejected.getByText("Resumen:")).toHaveCount(0);

	// The page in English.
	await page.getByRole("button", { name: "en", exact: true }).click();
	await expect(page.getByRole("heading", { name: "Your notes" })).toBeVisible();
	await expect(card.getByRole("list", { name: "Tags" }).getByRole("listitem")).toHaveText(["work", "shopping"]);
});

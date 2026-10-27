import type { Metadata } from "next";
import type { ReactNode } from "react";
import { Header } from "@/components/Header";
import { I18nProvider } from "@/lib/i18n";
import "./globals.css";

export const metadata: Metadata = {
	title: "dotnet-ai-template",
	description: "Notes with an AI summary: the example slice of a .NET 10 + AI template.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
	return (
		<html lang="es">
			<body className="min-h-screen antialiased">
				<I18nProvider>
					<Header />
					<main className="mx-auto w-full max-w-5xl px-4 pb-16 sm:px-6">{children}</main>
				</I18nProvider>
			</body>
		</html>
	);
}

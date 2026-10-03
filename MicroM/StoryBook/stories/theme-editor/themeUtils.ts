import { DEFAULT_THEME, MantineThemeOverride } from "@mantine/core";

export type Path = (string | number)[];

export function getPath(obj: unknown, path: Path): unknown {
    let cur: unknown = obj;
    for (const k of path) {
        if (cur === null || cur === undefined || typeof cur !== "object") return undefined;
        cur = (cur as Record<string | number, unknown>)[k];
    }
    return cur;
}

function isEmptyObject(v: unknown) {
    return v !== null && typeof v === "object" && !Array.isArray(v) && Object.keys(v as object).length === 0;
}

// undefined or "" removes the key and any emptied parents, so exports only contain real overrides.
export function setPath<T extends object>(obj: T, path: Path, value: unknown): T {
    const [head, ...rest] = path;
    const source = (obj ?? {}) as Record<string | number, unknown>;
    const copy: Record<string | number, unknown> = Array.isArray(source) ? [...source] as unknown as Record<string | number, unknown> : { ...source };

    if (rest.length === 0) {
        if (value === undefined || value === "") delete copy[head];
        else copy[head] = value;
    } else {
        const child = setPath((source[head] ?? {}) as object, rest, value);
        if (isEmptyObject(child)) delete copy[head];
        else copy[head] = child;
    }
    return copy as T;
}

export function deepClone<T>(v: T): T {
    return JSON.parse(JSON.stringify(v ?? {})) as T;
}

export function effective<T>(theme: MantineThemeOverride, path: Path): T {
    const v = getPath(theme, path);
    return (v !== undefined ? v : getPath(DEFAULT_THEME, path)) as T;
}

export function toIdentifier(name: string) {
    const id = name.trim().replace(/[^a-zA-Z0-9_$]+(.)?/g, (_, c: string | undefined) => (c ? c.toUpperCase() : "")).replace(/^[^a-zA-Z_$]+/, "");
    return id || "myTheme";
}

export function prettyJson(value: unknown, indent = 4, level = 0): string {
    const pad = " ".repeat(indent * (level + 1));
    const end = " ".repeat(indent * level);
    if (Array.isArray(value)) {
        if (value.every(v => v === null || typeof v !== "object")) return `[${value.map(v => JSON.stringify(v)).join(", ")}]`;
        return `[\n${value.map(v => pad + prettyJson(v, indent, level + 1)).join(",\n")}\n${end}]`;
    }
    if (value !== null && typeof value === "object") {
        const entries = Object.entries(value as Record<string, unknown>).filter(([, v]) => v !== undefined);
        if (!entries.length) return "{}";
        return `{\n${entries.map(([k, v]) => `${pad}${JSON.stringify(k)}: ${prettyJson(v, indent, level + 1)}`).join(",\n")}\n${end}}`;
    }
    return JSON.stringify(value);
}

export function themeToTypeScript(theme: MantineThemeOverride, exportName: string) {
    const name = toIdentifier(exportName);
    const json = prettyJson(theme);
    return `import { MantineThemeOverride } from "@mantine/core";

/**
 * Generated with the MicroM Storybook Theme Editor.
 * Usage:
 *   <MantineProvider theme={{ ...${name}, colorScheme }} withGlobalStyles withNormalizeCSS>
 */
export const ${name}: MantineThemeOverride = ${json};
`;
}

export function downloadText(filename: string, content: string, mime = "text/plain") {
    const blob = new Blob([content], { type: `${mime};charset=utf-8` });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

export function parseThemeText(text: string): MantineThemeOverride {
    // Strip comments first: the exported header contains "theme={{".
    const trimmed = text.replace(/\/\*[\s\S]*?\*\//g, "").replace(/^\s*\/\/.*$/gm, "").trim();
    let candidate = trimmed;
    if (!trimmed.startsWith("{")) {
        const eq = trimmed.search(/=\s*\{/);
        const start = eq >= 0 ? trimmed.indexOf("{", eq) : trimmed.indexOf("{");
        const end = trimmed.lastIndexOf("}");
        if (start < 0 || end < start) throw new Error("No theme object found");
        candidate = trimmed.substring(start, end + 1);
    }
    const parsed = JSON.parse(candidate);
    if (parsed === null || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error("The theme must be an object");
    return parsed as MantineThemeOverride;
}

import { Tuple } from "@mantine/core";

export type Palette = Tuple<string, 10>;

function hexToRgb(hex: string): [number, number, number] | null {
    const m = hex.trim().replace("#", "");
    const full = m.length === 3 ? m.split("").map(c => c + c).join("") : m;
    if (!/^[0-9a-fA-F]{6}$/.test(full)) return null;
    return [parseInt(full.slice(0, 2), 16), parseInt(full.slice(2, 4), 16), parseInt(full.slice(4, 6), 16)];
}

function rgbToHsl(r: number, g: number, b: number): [number, number, number] {
    r /= 255; g /= 255; b /= 255;
    const max = Math.max(r, g, b), min = Math.min(r, g, b);
    let h = 0, s = 0;
    const l = (max + min) / 2;
    if (max !== min) {
        const d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        switch (max) {
            case r: h = (g - b) / d + (g < b ? 6 : 0); break;
            case g: h = (b - r) / d + 2; break;
            default: h = (r - g) / d + 4;
        }
        h /= 6;
    }
    return [h * 360, s, l];
}

function hslToHex(h: number, s: number, l: number) {
    h = ((h % 360) + 360) % 360 / 360;
    const hue2rgb = (p: number, q: number, t: number) => {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1 / 6) return p + (q - p) * 6 * t;
        if (t < 1 / 2) return q;
        if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
        return p;
    };
    let r, g, b;
    if (s === 0) { r = g = b = l; }
    else {
        const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        const p = 2 * l - q;
        r = hue2rgb(p, q, h + 1 / 3);
        g = hue2rgb(p, q, h);
        b = hue2rgb(p, q, h - 1 / 3);
    }
    const toHex = (x: number) => Math.round(Math.min(1, Math.max(0, x)) * 255).toString(16).padStart(2, "0");
    return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

const LIGHTNESS = [0.96, 0.90, 0.81, 0.71, 0.62, 0.54, 0.46, 0.38, 0.30, 0.22];

export function isValidHex(hex: string) {
    return hexToRgb(hex) !== null;
}

// Keeps baseHex exactly at baseIndex and shifts the lightness curve of the remaining shades around it.
export function generatePalette(baseHex: string, baseIndex = 6): Palette {
    const rgb = hexToRgb(baseHex);
    if (!rgb) throw new Error(`Invalid color: ${baseHex}`);
    const [h, s, l] = rgbToHsl(...rgb);
    const offset = l - LIGHTNESS[baseIndex];
    const shades = LIGHTNESS.map((target, i) => {
        if (i === baseIndex) return baseHex.toLowerCase().startsWith("#") ? baseHex.toLowerCase() : `#${baseHex.toLowerCase()}`;
        const weight = 1 - Math.abs(i - baseIndex) / 10;
        const light = Math.min(0.98, Math.max(0.08, target + offset * weight));
        const sat = Math.min(1, s * (i < 3 ? 0.85 + i * 0.05 : 1));
        return hslToHex(h, sat, light);
    });
    return shades as Palette;
}

export function readableTextColor(bgHex: string) {
    const rgb = hexToRgb(bgHex);
    if (!rgb) return "#000";
    const [r, g, b] = rgb.map(v => {
        const c = v / 255;
        return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    });
    const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    return lum > 0.45 ? "#000" : "#fff";
}

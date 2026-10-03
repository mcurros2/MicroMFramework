import { MantineThemeOverride } from "@mantine/core";
import { useSyncExternalStore } from "react";

const STORAGE_KEY = "microm-storybook:editor-theme";

export type EditorTheme = MantineThemeOverride;

const listeners = new Set<() => void>();

function read(): EditorTheme {
    try {
        const raw = window.localStorage.getItem(STORAGE_KEY);
        if (raw) return JSON.parse(raw) as EditorTheme;
    } catch {
        // localStorage unavailable or invalid JSON
    }
    return {};
}

let current: EditorTheme = typeof window !== "undefined" ? read() : {};

export function getEditorTheme() {
    return current;
}

export function setEditorTheme(theme: EditorTheme) {
    current = theme;
    try {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(theme));
    } catch {
        // keep in memory only
    }
    listeners.forEach(l => l());
}

export function subscribeEditorTheme(listener: () => void) {
    listeners.add(listener);
    return () => { listeners.delete(listener); };
}

export function useEditorTheme() {
    return useSyncExternalStore(subscribeEditorTheme, getEditorTheme, getEditorTheme);
}

if (typeof window !== "undefined") {
    window.addEventListener("storage", e => {
        if (e.key === STORAGE_KEY) {
            current = read();
            listeners.forEach(l => l());
        }
    });
}

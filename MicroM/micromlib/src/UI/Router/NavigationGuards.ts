import type { FormMode } from "../Core/types";

export interface LocalNavigationIntent {
    currentRoute: string;
    nextRoute: string;
}

/* `disabled` bypasses protection. `allways` protects regardless of dirty state. */
export type NavigationProtectionMode = 'save' | 'confirm' | 'allways' | 'disabled';

export type NavigationProtection = Partial<Record<FormMode, NavigationProtectionMode>> & {
    default: NavigationProtectionMode,
};

export const DefaultNavigationProtection: NavigationProtection = {
    view: 'disabled',
    default: 'confirm',
};

export function resolveNavigationProtectionMode(navigationProtection: NavigationProtection | undefined, formMode: FormMode,): NavigationProtectionMode {
    const protection = navigationProtection ?? DefaultNavigationProtection;
    return protection[formMode] ?? protection.default;
}

export type LocalNavigationGuard = (intent: LocalNavigationIntent) => boolean | Promise<boolean>;

const localNavigationGuards = new Map<symbol, { guard: LocalNavigationGuard, scope?: string }>();

export function registerLocalNavigationGuard(guard: LocalNavigationGuard, scope?: string): () => void {
    const token = Symbol();
    localNavigationGuards.set(token, { guard, scope });
    return () => { localNavigationGuards.delete(token); };
}

export async function canNavigateLocally(intent: LocalNavigationIntent, scope?: string): Promise<boolean> {
    for (const [token, entry] of [...localNavigationGuards.entries()].reverse()) {
        if (entry.scope !== scope || !localNavigationGuards.has(token)) continue;
        if (!await entry.guard(intent)) return false;
    }
    return true;
}

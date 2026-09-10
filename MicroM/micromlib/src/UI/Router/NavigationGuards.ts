export interface LocalNavigationIntent {
    currentRoute: string;
    nextRoute: string;
}

/** `disabled` bypasses protection. `allways` skips dirty checks only in edit; view is unprotected. */
export type NavigationProtectionMode = 'save' | 'confirm' | 'allways' | 'disabled';

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

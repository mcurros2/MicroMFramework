export interface LocalNavigationIntent {
    currentRoute: string;
    nextRoute: string;
}

/** `allways` skips dirty/value checks only in edit mode; add uses those checks and view is unprotected. */
export type NavigationProtectionMode = 'save' | 'confirm' | 'allways';

export type LocalNavigationGuard = (intent: LocalNavigationIntent) => boolean | Promise<boolean>;

let localNavigationGuard: LocalNavigationGuard | null = null;

export function registerLocalNavigationGuard(guard: LocalNavigationGuard): () => void {
    localNavigationGuard = guard;

    return () => {
        if (localNavigationGuard === guard) localNavigationGuard = null;
    };
}

export async function canNavigateLocally(intent: LocalNavigationIntent): Promise<boolean> {
    const guard = localNavigationGuard;
    return guard ? await guard(intent) : true;
}

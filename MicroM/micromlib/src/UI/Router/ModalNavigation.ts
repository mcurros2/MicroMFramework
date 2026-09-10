// The router does not depend on React modal context (provider order is irrelevant).
interface ModalNavigator {
    hasModals: () => boolean;
    requestClose: () => Promise<void>;
    restoreRoute: () => Promise<void>;
}

const navigators = new Set<ModalNavigator>();

export function registerModalNavigator(navigator: ModalNavigator): () => void {
    navigators.add(navigator);
    return () => { navigators.delete(navigator); };
}

/** Consume navigation as a close request. Hash changes must first undo their new entry. */
export function consumeModalNavigation(hashChanged = false): boolean {
    const navigator = [...navigators].reverse().find(item => item.hasModals());
    if (!navigator) return false;
    void (async () => {
        if (hashChanged) await navigator.restoreRoute();
        await navigator.requestClose();
    })().catch(console.error);
    return true;
}

const stateKey = '__micromModal';
interface ModalHistoryState { session: string; position: number; }

/** Same-URL entries only; never serializes React content or reopens closed forms. */
export class ModalHistory {
    private readonly session = Math.random().toString(36).slice(2);
    private readonly entries = new Map<number, string>();
    private position = 0;
    private initialized = false;
    private busy = false;
    private disposed = false;
    private url = '';
    private operations: Promise<void> = Promise.resolve();
    private traversal?: { target: number; resolve: () => void; promise: Promise<void> };

    constructor(private readonly requestClose: () => Promise<void>, private readonly host: Window = window) {
        host.addEventListener('popstate', this.onPopState);
    }

    private marker(): ModalHistoryState | undefined {
        const marker = this.host.history.state?.[stateKey] as ModalHistoryState | undefined;
        return marker?.session === this.session ? marker : undefined;
    }

    private state(position: number) {
        return { ...this.host.history.state, [stateKey]: { session: this.session, position } };
    }

    private enqueue(operation: () => Promise<void>): Promise<void> {
        const pending = this.operations.then(operation);
        this.operations = pending.catch(() => {});
        return pending;
    }

    open(id: string): Promise<void> {
        return this.enqueue(async () => {
            await this.idle();
            if (this.disposed) return;
            if (!this.initialized || !this.marker()) {
                this.position = 0;
                this.entries.clear();
                this.host.history.replaceState(this.state(0), '', this.host.location.href);
                this.initialized = true;
            }
            this.url = this.host.location.href;
            // A push replaces the browser's forward branch.
            for (const position of this.entries.keys()) {
                if (position > this.position) this.entries.delete(position);
            }
            this.position++;
            this.entries.set(this.position, id);
            this.host.history.pushState(this.state(this.position), '', this.url);
        });
    }

    close(id: string): Promise<void> {
        return this.enqueue(async () => {
            await this.idle();
            const entry = [...this.entries].find(([, value]) => value === id);
            if (!entry) return;
            this.entries.delete(entry[0]);
            if (entry[0] === this.position) {
                const previous = Math.max(0, ...[...this.entries.keys()].filter(position => position < this.position));
                await this.go(previous, this.position);
            }
        });
    }

    private async idle(): Promise<void> {
        while (this.traversal && !this.disposed) {
            await this.traversal.promise;
        }
    }

    private go(target: number, from: number): Promise<void> {
        if (this.disposed || target === from) return Promise.resolve();
        let resolve!: () => void;
        const promise = new Promise<void>(complete => { resolve = complete; });
        this.traversal = { target, resolve, promise };
        this.host.history.go(target - from);
        return promise;
    }

    /** A raw hash assignment adds an unmarked entry above the current modal. */
    async restoreRoute(): Promise<void> {
        if (this.traversal || this.host.location.href === this.url) return;
        await this.go(this.position, this.position + 1);
    }

    private onPopState = () => {
        const marker = this.marker();
        if (!marker || this.disposed) return;
        if (this.traversal) {
            const pending = this.traversal;
            if (marker.position !== pending.target) {
                this.host.history.go(pending.target - marker.position);
                return;
            }
            this.position = marker.position;
            this.traversal = undefined;
            pending.resolve();
            return;
        }
        const origin = this.position;
        if (marker.position === origin) return;
        const backwards = marker.position < origin;
        const shouldClose = backwards && this.entries.size > 0 && !this.busy;
        this.busy = true;
        // Restore before asking: Stay and failures preserve the complete history.
        void (async () => {
            await this.go(origin, marker.position);
            if (shouldClose && !this.disposed) await this.requestClose();
        })().catch(console.error).finally(() => { this.busy = false; });
    };

    dispose(): void {
        this.disposed = true;
        this.host.removeEventListener('popstate', this.onPopState);
        this.traversal?.resolve();
        this.traversal = undefined;
    }
}

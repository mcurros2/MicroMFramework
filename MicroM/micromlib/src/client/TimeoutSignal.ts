export class TimeoutSignal {
    private controller: AbortController;
    private timeoutId: ReturnType<typeof setTimeout> | undefined;
    public readonly signal: AbortSignal;

    constructor(timeoutMs: number, errorMessage: string = 'Request timed out') {
        this.controller = new AbortController();
        this.signal = this.controller.signal;

        this.timeoutId = setTimeout(() => {
            this.controller.abort(new DOMException(errorMessage, 'TimeoutError'));
        }, timeoutMs);
    }

    clear(): void {
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
            this.timeoutId = undefined;
        }
    }

    get aborted(): boolean {
        return this.signal.aborted;
    }

    get abortReason(): any {
        return this.signal.reason;
    }
}

export function isIn<T>(value: T, ...params: T[]): boolean {
    if (params == null) return false;
    for (const v of params) {
        if (v != null && v != undefined && v === value) return true;
    }
    return false;
}

export function isPromise<T>(obj: unknown): obj is Promise<T> {
    return !!obj && typeof (obj as Promise<T>).then === 'function';
}

export function createKeysSet<T>(): Set<keyof T> {
    return new Set<keyof T>();
}

export function createKeysArray<T>(): Array<keyof T> {
    return new Array<keyof T>();
}

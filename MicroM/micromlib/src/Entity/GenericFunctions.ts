
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

export function nameof<T>(
    _type: new (...args: any[]) => T,
    selector: (x: T) => any
): string {
    const path: PropertyKey[] = [];

    const proxy = new Proxy({}, {
        get(_target, prop) {
            path.push(prop);
            return proxy;
        }
    }) as T;

    selector(proxy);

    return String(path[path.length - 1]);
}
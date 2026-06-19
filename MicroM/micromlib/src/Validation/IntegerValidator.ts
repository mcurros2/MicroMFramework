import { ReactNode } from "react";


export const isInteger = (error?: ReactNode) => {
    const _error: ReactNode | boolean | null = error || true;

    return (value: unknown) => {
        // Allways allow empty values. The required validation should be used for that.
        if (!value) return null;

        if (typeof value === 'number' && Number.isInteger(value)) {
            return null;
        }

        if (typeof value === 'string' && /^\s*-?\d+\s*$/.test(value)) {
            return null;
        }

        return _error;
    }

}
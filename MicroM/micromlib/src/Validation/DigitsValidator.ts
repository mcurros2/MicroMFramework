import { ReactNode } from "react";


export const isDigits = (error?: ReactNode) => {
    const _error: ReactNode | boolean | null = error || true;

    return (value: unknown) => {

        // Allways allow empty values. The required validation should be used for that.
        if (!value) return null;

        if (typeof value === 'number' && /^\d+$/.test(value.toString())) {
            return null;
        }

        if (typeof value === 'string' && /^\d+$/.test(value)) {
            return null;
        }

        return _error;
    }

}
import { ReactNode } from "react";
import { ValidatorFunction } from "./validationTypes";

export const isURL: ValidatorFunction = (error?: ReactNode) => {
    const _error: ReactNode | boolean | null = error || true;

    return (value: unknown) => {
        // Allways allow empty values. The required validation should be used for that.
        if (!value) return null;

        if (typeof value !== 'string') {
            return _error;
        }

        const pattern = new RegExp(
            '^https?://[^\\s]+[^\\s]$',
            'i'
        );


        return pattern.test(value) ? null : _error;
    }
}
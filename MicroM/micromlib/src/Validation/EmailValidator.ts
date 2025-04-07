import { ReactNode } from "react";
import { ValidatorFunction } from "./validationTypes";

export const isValidEmail: ValidatorFunction = (error?: ReactNode) => {
    const _error: ReactNode | boolean | null = error || true;

    return (value: unknown) => {
        // Allways allow empty values. The required validation should be used for that.
        if (!value) return null;

        if (typeof value !== 'string') {
            return _error;
        }


        const pattern = new RegExp(
            /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/
        );

        return pattern.test(value) ? null : _error;
    }
}
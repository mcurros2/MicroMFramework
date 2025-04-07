import { ReactNode } from "react";
import { ValidatorFunction } from "./validationTypes";

export const isPhone: ValidatorFunction = (error?: ReactNode) => {
    const _error: ReactNode | boolean | null = error || true;

    return (value: unknown) => {
        // Allways allow empty values. The required validation should be used for that.
        if (!value) return null;

        if (typeof value !== 'string') {
            return _error;
        }


        const pattern = new RegExp(
            '^\\+?\\d+$'
        );

        return pattern.test(value) ? null : _error;
    }
}
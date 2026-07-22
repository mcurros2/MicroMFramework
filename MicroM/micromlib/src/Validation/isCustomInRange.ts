import { ReactNode } from "react";
import { ValidatorFunction } from "./validationTypes";

interface IsCustomRangePayload {
    min: number,
    max: number
}

export const isCustomInRange: ValidatorFunction = ({ min, max }: IsCustomRangePayload, error?: ReactNode) => {
    const _error: ReactNode | boolean | null = error || true;

    return (value: unknown) => {
        // Allways allow empty values. The required validation should be used for that.
        if (!value) return null;

        if (typeof value !== 'string' && typeof value !== 'number') {
            return _error;
        }

        let valid = true;

        if(typeof value === 'string') {
            value = parseFloat(value);
        }

        if (typeof value !== 'number') {
            return _error;
        }

        if (value < min) valid = false;
        if (value > max) valid = false;

        return valid ? null : _error;
    }
}
import { ReactNode } from "react";
import { ValidatorFunction } from "./validationTypes";

export const isValidCUIT: ValidatorFunction = (error?: ReactNode) => {
    const _error: ReactNode | boolean | null = error || true;

    return (value: unknown) => {
        // Allways allow empty values. The required validation should be used for that.
        if (!value) return null;

        if (typeof value !== 'string') {
            return _error;
        }

        const validPrefixes = ['20', '23', '24', '27', '30', '33', '34', '37', '80', '83', '84', '87'];

        // Convert string to array of digits
        const cuitNumbers = value.replace(/-/g, '').split('').map(Number);

        if (cuitNumbers.length !== 11) return _error;

        const prefix = cuitNumbers.slice(0, 2).join('');
        if (!validPrefixes.includes(prefix)) return _error;

        const verifierDigit = cuitNumbers.pop() as number;

        // Compute verifier digit
        const weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        const multiplied = cuitNumbers.map((digit, i) => {
            return digit * weights[i];
        });
        const suma = multiplied.reduce((sum, multiplier) => {
            return sum + multiplier;
        });
        const modulo = suma % 11;
        const computedVerifier = (11 - modulo) % 11;

        return computedVerifier === verifierDigit ? null : _error;
    }
}
/* eslint-disable react/display-name */
import { ReactNode } from "react";
import { ValidationRule } from "./validationTypes";

export const combineValidators = (Container?: React.ComponentType<{ children: ReactNode }>, ...validators: ValidationRule[]): ValidationRule => {
    return (value: unknown, values?: Record<string, unknown>) => {
        const errorMessages: ReactNode[] = [];
        let hasTrueError = false;

        validators.forEach(validator => {
            const result = validator(value, values);
            if (result && result !== true) {
                errorMessages.push(result as ReactNode);
            } else if (result === true) {
                hasTrueError = true;
            }
        });

        if (errorMessages.length === 0) {
            return hasTrueError ? true : null;
        }

        if (!Container) {
            return errorMessages;
        }

        return <Container>{errorMessages}</Container>;
    }
}

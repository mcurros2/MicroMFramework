import { ValidationRule, ValidatorFunction } from "./validationTypes";

export const CustomValidator: ValidatorFunction = (validationFunction: ValidationRule) => {
    return validationFunction;
}
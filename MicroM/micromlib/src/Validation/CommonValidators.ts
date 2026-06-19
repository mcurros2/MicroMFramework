import { hasLength, isInRange, isNotEmpty, matches, matchesField } from "@mantine/form";
import { ReactNode } from "react";
import { isValidCUIT } from "./CUITValidator";
import { CustomValidator } from "./CustomValidator";
import { isDigits } from "./DigitsValidator";
import { isValidEmail } from "./EmailValidator";
import { isInteger } from "./IntegerValidator";
import { isPhone } from "./PhoneValidator";
import { isURL } from "./UrlValidator";
import { ValidatorFunction } from "./validationTypes";

export const CommonValidators = {
    // Custom Regex
    url: isURL,
    phone: isPhone,
    cuit: isValidCUIT,
    email: isValidEmail,
    digits: isDigits,
    integer: isInteger,
    // From mantine
    length: hasLength,
    required: isNotEmpty,
    regex: matches,
    range: isInRange,
    field: matchesField as ValidatorFunction,
    custom: CustomValidator
};

export type CommonValidatorsType = Record<keyof typeof CommonValidators, ValidatorFunction>;

export type ValidatorConfigurationParms = {
    message?: ReactNode;
    data?: any;
};

export type ValidatorConfiguration = Partial<Record<keyof typeof CommonValidators, ValidatorConfigurationParms>>;


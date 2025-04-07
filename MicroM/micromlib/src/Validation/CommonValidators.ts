import { hasLength, isInRange, isNotEmpty, matches, matchesField } from "@mantine/form";
import { isValidCUIT } from "./CUITValidator";
import { isValidEmail } from "./EmailValidator";
import { isPhone } from "./PhoneValidator";
import { isURL } from "./UrlValidator";
import { ValidatorFunction } from "./validationTypes";
import { CustomValidator } from "./CustomValidator";
import { ReactNode } from "react";

export const CommonValidators = {
    // Custom Regex
    url: isURL,
    phone: isPhone,
    cuit: isValidCUIT,
    email: isValidEmail,
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


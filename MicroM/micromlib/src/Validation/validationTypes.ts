import { JSXElementConstructor, ReactElement, ReactNode } from "react";

export type ValidationRule = (value: unknown, values?: Record<string, unknown>) => string | number | true | ReactElement<any, string | JSXElementConstructor<any>> | Iterable<ReactNode> | null;
export type ValidatorFunction = (...args: any[]) => ValidationRule;



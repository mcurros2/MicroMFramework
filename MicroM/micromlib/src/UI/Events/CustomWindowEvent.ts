import type { ValuesObject } from "../../client/client.types";

export interface CustomWindowEventDefinition {
    eventName: string;
    payload: ValuesObject;
}

export type CustomWindowEvent<T extends CustomWindowEventDefinition> = T;

export function triggerCustomWindowEvent<T extends CustomWindowEventDefinition>(
    eventName: T["eventName"],
    payload: T["payload"]
): void {
    window.dispatchEvent(new CustomEvent<T["payload"]>(eventName, { detail: payload }));
}
import { Button, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, useEffect, useMemo, useRef, useState } from "react";
import { Entity, EntityDefinition } from "../../../Entity";
import { MicroMClient } from "../../../client/MicromClient";
import { ValuesObject } from "../../../client/client.types";
import { AlertError } from "../../Core/AlertError";
import { useModal } from "../../Core/ModalsManager";
import { EntitySourceProps } from "../../GetEntity/GetEntity";
import { useResolvedEntityConstructor } from "../../GetEntity/useResolvedEntityConstructor";
import { MenuModalCloseBehavior, useMenuModalCloseNavigator } from "../useMenuModalCloseNavigator/useMenuModalCloseNavigator";

export type MenuEntityActionModalProps = EntitySourceProps & {
    client: MicroMClient;
    parentKeys?: ValuesObject;
    actionName: string;
    selectedKeys?: ValuesObject[];
    closeBehavior?: MenuModalCloseBehavior;
    errorComponent?: (error: unknown, close: () => void) => ReactNode;
    errorTitle?: string;
    backLabel?: string;
    actionNotFoundLabel?: string;
    selectionRequiredLabel?: string;
};

export const MenuEntityActionModalDefaultProps: Partial<MenuEntityActionModalProps> = {
    errorTitle: "Could not execute the action",
    backLabel: "Back",
    actionNotFoundLabel: "Action not found",
    selectionRequiredLabel: "This action requires selecting records and is not available from this menu.",
    selectedKeys: []
};

export function MenuEntityActionModal(props: MenuEntityActionModalProps) {
    const mergedProps = useComponentDefaultProps("MenuEntityActionModal", MenuEntityActionModalDefaultProps, props);
    const {
        client, parentKeys, actionName, selectedKeys, closeBehavior, errorComponent,
        errorTitle, backLabel, actionNotFoundLabel, selectionRequiredLabel
    } = mergedProps;

    const modal = useModal();

    const handleMenuClose = useMenuModalCloseNavigator(closeBehavior);

    const { entityConstructor: resolvedCtor, loading, error: loadError } = useResolvedEntityConstructor(client, parentKeys, mergedProps);

    const entity = useMemo<Entity<EntityDefinition> | null>(
        () => (resolvedCtor ? resolvedCtor(client) : null),
        [resolvedCtor, client]
    );

    const [configError, setConfigError] = useState<string | null>(null);
    const [executionError, setExecutionError] = useState<unknown>(null);

    const hasExecutedRef = useRef(false);

    useEffect(() => {
        if (!entity || hasExecutedRef.current) return;

        const resolvedAction = entity.def.clientActions?.[actionName];

        if (!resolvedAction) {
            const message = `${actionNotFoundLabel} "${actionName}"`;
            console.warn(`MenuEntityActionModal: action "${actionName}" not found in entity "${entity.def.name}".`);
            hasExecutedRef.current = true;
            setConfigError(message);
            return;
        }

        const selectionOk = resolvedAction.dontRequireSelection || (
            selectedKeys!.length > 0 &&
            (resolvedAction.minSelectedRecords === undefined || selectedKeys!.length >= resolvedAction.minSelectedRecords) &&
            (resolvedAction.maxSelectedRecords === undefined || selectedKeys!.length <= resolvedAction.maxSelectedRecords)
        );

        if (!selectionOk) {
            console.warn(`MenuEntityActionModal: action "${resolvedAction.name}" requires selected records and is not available from this menu.`);
            hasExecutedRef.current = true;
            setConfigError(selectionRequiredLabel ?? null);
            return;
        }

        hasExecutedRef.current = true;

        resolvedAction.onClick({
            entity,
            modal,
            selectedKeys,
            onClose: async (result?: boolean) => {
                handleMenuClose();
                return result ?? false;
            }
        }).catch(err => {
            console.error(`MenuEntityActionModal: error executing action "${resolvedAction.name}"`, err);
            setExecutionError(err);
        });
    }, [entity, actionName, selectedKeys, modal, handleMenuClose, actionNotFoundLabel, selectionRequiredLabel]);

    const error = loadError ?? configError ?? executionError;

    if (error) {
        if (errorComponent) return <>{errorComponent(error, handleMenuClose)}</>;
        return (
            <>
                <AlertError title={errorTitle} mt="md">
                    {typeof error === "string" ? error : (error instanceof Error ? error.message : String(error))}
                </AlertError>
                <Button size="xs" mt="xs" onClick={handleMenuClose}>{backLabel}</Button>
            </>
        );
    }

    if (loading || !entity) return null;

    return null;
}

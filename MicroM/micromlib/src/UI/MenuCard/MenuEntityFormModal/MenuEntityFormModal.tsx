import { Button, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, useState } from "react";
import { MicroMClient } from "../../../client/MicromClient";
import { ValuesObject } from "../../../client/client.types";
import { AlertError } from "../../Core/AlertError";
import { EntityFormModal, EntityFormModalProps } from "../../Form/EntityFormModal";
import { EntitySourceProps } from "../../GetEntity/GetEntity";
import { useResolvedEntityConstructor } from "../../GetEntity/useResolvedEntityConstructor";
import { MenuModalCloseBehavior, useMenuModalCloseNavigator } from "../useMenuModalCloseNavigator/useMenuModalCloseNavigator";

type PassthroughProps = Omit<EntityFormModalProps, "entityConstructor" | "openState" | "setOpenState" | "onModalClosed" | "client">;

export type MenuEntityFormModalProps = PassthroughProps & EntitySourceProps & {
    client: MicroMClient;
    parentKeys?: ValuesObject;
    closeBehavior?: MenuModalCloseBehavior;
    errorComponent?: (error: unknown, close: () => void) => ReactNode;
    errorTitle?: string;
    backLabel?: string;
};

export const MenuEntityFormModalDefaultProps: Partial<MenuEntityFormModalProps> = {
    errorTitle: "Could not open the form",
    backLabel: "Back",
};

export function MenuEntityFormModal(props: MenuEntityFormModalProps) {
    const mergedProps = useComponentDefaultProps("MenuEntityFormModal", MenuEntityFormModalDefaultProps, props);
    const {
        client, parentKeys, entityConstructor, entityLoader, closeBehavior, errorComponent, errorTitle, backLabel, ...rest
    } = mergedProps;

    const [openState, setOpenState] = useState(true);

    const { entityConstructor: resolved, loading, error } = useResolvedEntityConstructor(client, parentKeys, mergedProps);

    const handleMenuClose = useMenuModalCloseNavigator(closeBehavior);

    if (error) {
        console.error("MenuEntityFormModal: error resolving entity", error);
        if (errorComponent) return <>{errorComponent(error, handleMenuClose)}</>;
        return (
            <>
                <AlertError title={errorTitle} mt="md">{error instanceof Error ? error.message : String(error)}</AlertError>
                <Button size="xs" mt="xs" onClick={handleMenuClose}>{backLabel}</Button>
            </>
        );
    }

    if (loading || !resolved) return null;

    return (
        <EntityFormModal
            {...rest}
            client={client}
            entityConstructor={resolved}
            openState={openState}
            setOpenState={setOpenState}
            onModalClosed={handleMenuClose}
        />
    );
}

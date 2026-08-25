import { Card, CardProps, DefaultMantineColor, Skeleton, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { EntityFormPage, EntityFormPageProps } from ".";
import { ValuesObject } from "../../client/client.types";
import { EntitySourceProps } from "../GetEntity/GetEntity";
import { useResolvedEntityConstructor } from "../GetEntity/useResolvedEntityConstructor";

type EntityFormPanelBaseProps = Omit<EntityFormPageProps, "entityConstructor">;

export type EntityFormPanelProps = EntityFormPanelBaseProps & EntitySourceProps & {
    parentKeys?: ValuesObject;
    bgLight?: DefaultMantineColor;
    bgDark?: DefaultMantineColor;
    /** Props for the wrapping Card. Replaces the default object entirely when set per instance */
    containerCardProps?: Omit<CardProps, 'children'>;
};

export const EntityFormPanelDefaultProps: Partial<EntityFormPanelProps> = {
    saveAndGetOnSubmit: true,
    defaultLoadingComponent: <Skeleton />,
    bgLight: 'gray.3',
    bgDark: 'dark.8',
    navigationProtection: 'confirm',
    containerCardProps: { h: '100%', withBorder: true },
};

export function EntityFormPanel(props: EntityFormPanelProps) {
    const mergedProps = useComponentDefaultProps("EntityFormPanel", EntityFormPanelDefaultProps, props);
    const {
        client, parentKeys, entityConstructor, entityLoader, defaultLoadingComponent, bgLight, bgDark, navigationProtection, containerCardProps, ...rest
    } = mergedProps;

    const theme = useMantineTheme();

    const { entityConstructor: resolvedEntityConstructor } =
        useResolvedEntityConstructor(client, parentKeys, mergedProps);

    return (
        <Card bg={theme.colorScheme === "light" ? bgLight : bgDark} {...containerCardProps}>
            {!resolvedEntityConstructor ? (
                defaultLoadingComponent
            ) :
                <EntityFormPage
                    {...rest}
                    client={client}
                    defaultLoadingComponent={defaultLoadingComponent}
                    entityConstructor={resolvedEntityConstructor}
                    navigationProtection={navigationProtection}
                />
            }
        </Card>
    );
}
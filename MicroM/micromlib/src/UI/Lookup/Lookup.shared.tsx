import { MantineSize } from "@mantine/core";
import { IconSearch } from "@tabler/icons-react";
import { ValuesObject } from "../../client";
import { Entity, EntityDefinition } from "../../Entity";
import { ActionIconVariant, useTextTransformProps } from "../Core";
import { UseEntityFormReturnType } from "../Form";
import { LookupResultState } from "./useLookup";

export interface LookupCommonProps extends Omit<useTextTransformProps, 'entityForm' | 'column'> {
    parentKeys?: ValuesObject,
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean
    entityForm: UseEntityFormReturnType
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
    required?: boolean,
    readonly?: boolean,
    disabled?: boolean,
    label?: string,
    idMaxWidth?: string,
    icon?: React.ReactNode,
    iconVariant?: ActionIconVariant,
    requiredLabel?: string
    description?: string,
    size?: MantineSize
    onLookupPerformed?: (lookupResult: LookupResultState) => void,
    enableAdd?: boolean,
    enableEdit?: boolean,
    enableDelete?: boolean,
    enableView?: boolean,
}

export const LookupDefaultProps: Partial<LookupCommonProps> = {
    idMaxWidth: "15rem",
    icon: <IconSearch size="1rem" stroke="1.5" />,
    iconVariant: "light",
    requiredLabel: "A value is required",
    size: "sm",
    enableAdd: false,
    enableEdit: false,
    enableDelete: false,
    enableView: true,
    autoTrim: true,
};

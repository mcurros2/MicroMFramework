import { MantineNumberSize } from "@mantine/core";
import { Value } from "../../client";
import { EntityColumn } from "../../Entity";
import { CompoundLookup } from "./CompoundLookup";
import { LookupCommonProps, LookupDefaultProps } from "./Lookup.shared";
import { SingleLookup } from "./SingleLookup";

export type CompoundLookupProps = LookupCommonProps & { column?: never, bindingColumns: EntityColumn<Value>[], editLastLevelOnly?: boolean, prefixMaxWidth?: MantineNumberSize };

export type LookupProps = LookupCommonProps & (
    { column: EntityColumn<Value>, bindingColumns?: never } |
    CompoundLookupProps
);

export { LookupDefaultProps };

export function Lookup(props: LookupProps) {
    if (props.bindingColumns) return <CompoundLookup {...props} />;
    return <SingleLookup {...props} />;
}

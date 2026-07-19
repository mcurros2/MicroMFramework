import { Value } from "../../client";
import { EntityColumn } from "../../Entity";
import { CompoundLookup } from "./CompoundLookup";
import { LookupCommonProps, LookupDefaultProps } from "./Lookup.shared";
import { SingleLookup } from "./SingleLookup";

export type LookupProps = LookupCommonProps & (
    { column: EntityColumn<Value>, bindingColumns?: never } |
    { column?: never, bindingColumns: EntityColumn<Value>[], editLastLevelOnly?: boolean }
);

export { LookupDefaultProps };

export function Lookup(props: LookupProps) {
    if (props.bindingColumns) return <CompoundLookup {...props} />;
    return <SingleLookup {...props} />;
}

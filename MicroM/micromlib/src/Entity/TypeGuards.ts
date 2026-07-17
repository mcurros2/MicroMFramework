import { Value } from "../client";
import { EntityColumn } from "./EntityColumn";
import { EntityLookup, ViewMapping } from "./EntityLookup";

export function isInstanceOfEntityColumn<T extends Value>(obj: unknown): obj is EntityColumn<T> {
    return obj instanceof EntityColumn;
}

export function isViewMapping(obj: unknown): obj is ViewMapping {
    return (
        typeof obj === "object" &&
        obj !== null &&
        typeof (obj as ViewMapping).keyIndex === "number" &&
        typeof (obj as ViewMapping).descriptionIndex === "number"
    );
}

export function isEntityLookup(obj: unknown): obj is EntityLookup {
    // Basic checks for required properties
    const hasBasicProps =
        typeof obj === "object" &&
        obj !== null &&
        typeof (obj as EntityLookup).name === "string" &&
        typeof (obj as EntityLookup).entityConstructor === "function";

    // Optional properties
    const lookup = obj as EntityLookup;
    const hasView = lookup?.view === undefined || typeof lookup.view === "string";
    const hasProc = lookup?.proc === undefined || typeof lookup.proc === "string";
    const hasBindingColumnKey = lookup?.bindingColumnKey === undefined || typeof lookup.bindingColumnKey === "string";
    const hasCompoundKeyGroupName = lookup?.compoundKeyGroupName === undefined || typeof lookup.compoundKeyGroupName === "string";
    const hasViewMapping = lookup?.viewMapping === undefined || isViewMapping(lookup.viewMapping);

    return hasBasicProps && hasView && hasProc && hasBindingColumnKey && hasCompoundKeyGroupName && hasViewMapping;
}

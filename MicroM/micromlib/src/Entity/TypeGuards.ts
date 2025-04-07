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
    const hasView = !obj || typeof (obj as EntityLookup).view === "string";
    const hasProc = !obj || typeof (obj as EntityLookup).proc === "string";
    const hasBindingColumnKey = !obj || typeof (obj as EntityLookup).bindingColumnKey === "string";
    const hasViewMapping = !obj || isViewMapping((obj as EntityLookup).viewMapping);

    return hasBasicProps && hasView && hasProc && hasBindingColumnKey && hasViewMapping;
}

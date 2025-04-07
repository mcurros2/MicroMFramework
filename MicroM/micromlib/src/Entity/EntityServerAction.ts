import { ValuesObject } from "../client";
import { ColumnsObject } from "./EntityColumnCollection.types";
import { EntityDefinition } from "./EntityDefinition";

export interface EntityServerAction {
    name: string;
    valuesMapper?: (values: ValuesObject) => EntityDefinition;
    columnsMapper?: (columns: ColumnsObject) => ValuesObject;
}


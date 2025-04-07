import { Value } from "../client";
import { EntityColumn } from "./EntityColumn";

export type ColumnsObject = Record<string, EntityColumn<Value>>;

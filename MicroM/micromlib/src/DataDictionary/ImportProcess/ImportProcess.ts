import { MicroMClient, ValuesObject } from "../../client";
import { Entity, ImportDataDestinationProps } from "../../Entity";
import { ImportProcessDef } from "./ImportProcessDef";

export class ImportProcess extends Entity<ImportProcessDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}, destinationProps: ImportDataDestinationProps) {
        super(client, new ImportProcessDef(destinationProps), parentKeys);
        this.Form = null;
    }
}

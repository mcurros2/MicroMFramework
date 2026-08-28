import { MicroMClient, ValuesObject } from "../../client";
import { Entity } from "../../Entity";
import { ImportProcessDef } from "./ImportProcessDef";

export class ImportProcess extends Entity<ImportProcessDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new ImportProcessDef(), parentKeys);
        this.Form = null;
        this.Title = "Import Process";
    }
}

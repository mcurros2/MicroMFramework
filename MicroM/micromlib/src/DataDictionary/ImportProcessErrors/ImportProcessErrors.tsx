import { MicroMClient, ValuesObject } from "../../client";
import { Entity } from "../../Entity";
import { ImportProcessErrorsDef } from "./ImportProcessErrorsDef";

export class ImportProcessErrors extends Entity<ImportProcessErrorsDef> {
    constructor(client: MicroMClient, parentKeys: ValuesObject = {}) {
        super(client, new ImportProcessErrorsDef(), parentKeys);
        this.Form = null;
        this.Title = "Import Process Errors";
    }
}

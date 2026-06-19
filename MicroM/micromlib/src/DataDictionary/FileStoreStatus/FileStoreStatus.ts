import { MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { FileStoreStatusDef } from "./FileStoreStatusDef";

export class FileStoreStatus extends Entity<FileStoreStatusDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new FileStoreStatusDef(), parentKeys);
    }
}
import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { FileStoreStatusDef } from "./FileStoreStatusDef";

export class FileStoreStatus extends Entity<FileStoreStatusDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new FileStoreStatusDef(), parentKeys);
    }
}
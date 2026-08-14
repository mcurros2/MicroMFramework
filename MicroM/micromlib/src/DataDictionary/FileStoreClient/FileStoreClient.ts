import { MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { FileStoreClientDef } from "./FileStoreClientDef";

export class FileStoreClient extends Entity<FileStoreClientDef> {
    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new FileStoreClientDef(), parentKeys);
    }
}

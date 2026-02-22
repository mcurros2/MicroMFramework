import { MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { FileStoreProcessDef } from "./FileStoreProcessDef";

export class FileStoreProcess extends Entity<FileStoreProcessDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new FileStoreProcessDef(), parentKeys);
    }
}
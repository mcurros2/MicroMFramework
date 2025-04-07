import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { FileStoreDef } from "./FileStoreDef";

export class FileStore extends Entity<FileStoreDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new FileStoreDef(), parentKeys);
    }
}
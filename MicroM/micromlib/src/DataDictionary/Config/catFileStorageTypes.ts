import { MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { CategoriesValuesDef } from "../Categories";

export class catFileStorageTypes extends Entity<CategoriesValuesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new CategoriesValuesDef(), parentKeys);
        this.def.columns.c_category_id.value = "FileStorageTypes";
        this.def.columns.c_category_id.defaultValue = "FileStorageTypes";
        this.Title = "File Storage Types";
    }
}
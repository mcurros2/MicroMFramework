import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { CategoriesValuesDef } from "../Categories";

export class catUserTypes extends Entity<CategoriesValuesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new CategoriesValuesDef(), parentKeys);
        this.def.columns.c_category_id.value = "UserTypes";
        this.def.columns.c_category_id.defaultValue = "UserTypes";
        this.Title = "User Types";
    }
}
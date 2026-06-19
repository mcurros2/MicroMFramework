import { MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { CategoriesValuesDef } from "../Categories";

export class catAuthenticationTypes extends Entity<CategoriesValuesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new CategoriesValuesDef(), parentKeys);
        this.def.columns.c_category_id.value = "AuthenticationTypes";
        this.def.columns.c_category_id.defaultValue = "AuthenticationTypes";
        this.Title = "Authentication Types";
    }
}
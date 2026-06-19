import { MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { CategoriesValuesDef } from "../Categories";

export class catIdentityProviderRoles extends Entity<CategoriesValuesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new CategoriesValuesDef(), parentKeys);
        this.def.columns.c_category_id.value = "IdentityProviderRoles";
        this.def.columns.c_category_id.defaultValue = "IdentityProviderRoles";
        this.Title = "Identity Provider Roles";
    }
}
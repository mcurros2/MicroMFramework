import { MicroMClient } from "../../client";
import { Entity } from "../../Entity";
import { CategoriesDef } from "./CategoriesDef";

export class Categories extends Entity<CategoriesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new CategoriesDef(), parentKeys);
        this.Form = import('./CategoriesForm').then(module => module.CategoriesForm);
    }
}


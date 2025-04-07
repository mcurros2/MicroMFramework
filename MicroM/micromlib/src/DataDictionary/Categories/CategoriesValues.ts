import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { CategoriesValuesDef } from "./CategoriesValuesDef";

export class CategoriesValues extends Entity<CategoriesValuesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new CategoriesValuesDef(), parentKeys);
        this.Form = import('../../UI/AutoForm/AutoForm').then(module => module.AutoForm);
    }
}
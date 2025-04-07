import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { StatusValuesDef } from "./StatusValuesDef";


export class StatusValues extends Entity<StatusValuesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new StatusValuesDef(), parentKeys);
        this.Form = import('../../UI/AutoForm/AutoForm').then(module => module.AutoForm);
    }
}
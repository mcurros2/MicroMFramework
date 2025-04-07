import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { StatusDef } from "./StatusDef";


export class Status extends Entity<StatusDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new StatusDef(), parentKeys);
        //this.Form = import('./StatusForm').then((module) => module.StatusForm);
    }
}
import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { MicromUsersDef } from "./MicromUsersDef";

export class MicromUsers extends Entity<MicromUsersDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new MicromUsersDef(), parentKeys);
        this.Form = import('./MicromUsersForm').then((module) => module.MicromUsersForm);
    }
}
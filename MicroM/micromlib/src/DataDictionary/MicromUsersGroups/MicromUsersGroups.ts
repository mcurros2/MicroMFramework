import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { MicromUsersGroupsDef } from "./MicromUsersGroupsDef";


export class MicromUsersGroups extends Entity<MicromUsersGroupsDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new MicromUsersGroupsDef(), parentKeys);
        this.Form = import('./MicromUsersGroupsForm').then((module) => module.MicromUsersGroupsForm);
        this.Title = "Microm Users Groups";
    }
}
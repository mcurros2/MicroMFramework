import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { MicromUsersGroupsMembersDef } from "./MicromUsersGroupsMembersDef";


export class MicromUsersGroupsMembers extends Entity<MicromUsersGroupsMembersDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new MicromUsersGroupsMembersDef(), parentKeys);
        this.Form = 'AutoForm';
        this.Title = "Microm Users Groups Members";
    }
}
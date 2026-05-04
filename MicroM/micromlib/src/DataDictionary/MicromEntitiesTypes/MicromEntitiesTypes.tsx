import { MicroMClient } from "../../client/MicromClient";
import { Entity } from "../../Entity";
import { MicromEntitiesTypesDef } from "./MicromEntitiesTypesDef";

export class MicromEntitiesTypes extends Entity<MicromEntitiesTypesDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new MicromEntitiesTypesDef(), parentKeys);
        this.Title = "Microm Entities Types";
    }
}
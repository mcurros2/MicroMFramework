import { MicroMClient } from "../../client/MicromClient";
import { Entity } from "../../Entity";
import { MicromDeveloperToolsCodeGenDef } from "./MicromDeveloperToolsCodeGenDef";

export class MicromDeveloperToolsCodeGen extends Entity<MicromDeveloperToolsCodeGenDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new MicromDeveloperToolsCodeGenDef(), parentKeys);
        this.Form = import('./MicromDeveloperToolsCodeGenForm').then((module) => module.MicromDeveloperToolsCodeGenForm);
        this.Title = "Generated Code";
    }
}
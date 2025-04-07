import { IconCloudUpload, IconProps } from "@tabler/icons-react";
import { ReactNode } from "react";
import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { ImportEntityDataDef } from "./ImportEntityDataDef";

export interface ImportEntityDataLabels {
    Icon?: (props: IconProps) => ReactNode,
    Title?: string,
    HelpText?: string
}

export const ImportEntityDataLabels: Partial<ImportEntityDataLabels> = {
    Icon: IconCloudUpload,
    Title: 'Import Data',
    HelpText: 'Import a CSV file with data for this entity.'
}

export class ImportEntityData extends Entity<ImportEntityDataDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new ImportEntityDataDef(), parentKeys);
        this.Form = import('./ImportEntityDataForm').then((module) => module.ImportEntityDataForm);
        this.Title = ImportEntityDataLabels.Title!;
        this.HelpText = ImportEntityDataLabels.HelpText;
        this.Icon = ImportEntityDataLabels.Icon;
    }
}
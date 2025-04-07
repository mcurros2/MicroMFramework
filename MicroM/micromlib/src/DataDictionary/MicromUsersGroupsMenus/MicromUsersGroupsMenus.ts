import { IconProps } from "@tabler/icons-react";
import { ReactNode } from "react";
import { Entity } from "../../Entity";
import { MicroMClient } from "../../client";
import { MicromUsersGroupsMenusDisableMenusLabels } from "./ACTDisableMenus";
import { MicromUsersGroupsMenusEnableMenusLabels } from "./ACTEnableMenus";
import { MicromUsersGroupsMenusDef } from "./MicromUsersGroupsMenusDef";

export interface MicromUsersGroupsMenusEntityProps {
    icon?: (props: IconProps) => ReactNode,
    helpText?: string,
    title: string,
}

export const MicromUsersGroupsMenusEntityDefaultProps: Partial<MicromUsersGroupsMenusEntityProps> = {
    icon: undefined,
    helpText: '* The list of available menu items to be allowed or denied access',
    title: 'Menu access',
}

export class MicromUsersGroupsMenus extends Entity<MicromUsersGroupsMenusDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new MicromUsersGroupsMenusDef(), parentKeys);
        
        this.Title = MicromUsersGroupsMenusEntityDefaultProps.title!;
        this.Icon = MicromUsersGroupsMenusEntityDefaultProps.icon;
        this.HelpText = MicromUsersGroupsMenusEntityDefaultProps.helpText;

        this.def.clientActions.ACTEnableMenus.label = MicromUsersGroupsMenusEnableMenusLabels.buttonLabel;
        this.def.clientActions.ACTDisableMenus.label = MicromUsersGroupsMenusDisableMenusLabels.buttonLabel;

    }
}
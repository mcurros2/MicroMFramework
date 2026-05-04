import { Text } from "@mantine/core";
import { IconDeviceDesktopCode } from "@tabler/icons-react";
import { ReactNode } from "react";
import { EntityClientAction, EntityFormComponentPromise } from "../../Entity";
import { MicromDeveloperToolsCodeGen } from "../MicromDeveloperToolsCodeGen";
import { MicromEntitiesTypes } from "./MicromEntitiesTypes";

export interface ACTMicromGetGeneratedCodeProps {
    CodeGenerationLabel: string,
    TitleLabel: string,
    icon: ReactNode,
    FormTitle: string
}

export const ACTMicromGetGeneratedCodeDefaultProps: Partial<ACTMicromGetGeneratedCodeProps> = {
    CodeGenerationLabel: "Code Generation",
    TitleLabel: "Generated Code",
    icon: <IconDeviceDesktopCode size="1rem" />,
    FormTitle: "Generated Code"
}

export const ACTMicromGetGeneratedCode: EntityClientAction = {
    name: 'ACTMicromGetGeneratedCode',
    label: ACTMicromGetGeneratedCodeDefaultProps.CodeGenerationLabel,
    title: ACTMicromGetGeneratedCodeDefaultProps.TitleLabel,
    icon: ACTMicromGetGeneratedCodeDefaultProps.icon,
    showActionInViewMode: true,
    onClick: async ({ entity, modal, selectedKeys, element }) => {
        const types = entity as MicromEntitiesTypes;
        const code = new MicromDeveloperToolsCodeGen(types.API.client);
        const selected = selectedKeys?.[0][types.def.columns.vc_entity_name.name] as string;
        code.def.columns.vc_classname.value = selected || '';
        if (modal) {
            await modal.open({
                modalProps: {
                    title: <Text fw={700}>{ACTMicromGetGeneratedCodeDefaultProps.FormTitle}</Text>,
                    size: 'xl'
                },
                focusOnClosed: element,
                content: (code.Form! as EntityFormComponentPromise).then(EntityForm =>
                    <EntityForm
                        entity={code}
                        initialFormMode='view'
                        getDataOnInit={true}
                        onCancel={async () => await modal.close()}
                    />)
            });

        }
        return false;
    }
}
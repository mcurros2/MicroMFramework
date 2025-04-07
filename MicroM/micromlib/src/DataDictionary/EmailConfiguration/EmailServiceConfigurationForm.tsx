import { Button, Card, Divider, Group, Stack, Tabs, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconMailCheck, IconMailCog } from "@tabler/icons-react";
import { ReactNode } from "react";
import { CheckboxField, EntityForm, FormOptions, NumberField, PasswordField, TextAreaField, TextField, useEntityForm } from "../../UI";
import { EmailServiceConfiguration } from "./EmailServiceConfiguration";

export interface EmailServiceConfigurationFormProps extends FormOptions<EmailServiceConfiguration> {
    settingsTABLabel?: string,
    settingsTABIcon?: ReactNode,
    recoveryTABLabel?: string,
    recoveryTABIcon?: ReactNode,
    setDefaultMessageButtonLabel?: string,
}

export const EmailServiceConfigurationFormDefaultProps: Partial<EmailServiceConfigurationFormProps> = {
    initialFormMode: "view",
    settingsTABIcon: <IconMailCog size="1rem" />,
    settingsTABLabel: 'Settings',
    recoveryTABIcon: <IconMailCheck size="1rem" />,
    recoveryTABLabel: 'Recovery template',
    setDefaultMessageButtonLabel: 'Set default message',
}

const TN = {
    settings: 'settings',
    recovery: 'recovery',
};

export function EmailServiceConfigurationForm(props: EmailServiceConfigurationFormProps) {

    const {
        entity, initialFormMode, getDataOnInit, onSaved, onCancel, settingsTABLabel, settingsTABIcon, recoveryTABLabel, recoveryTABIcon,
        setDefaultMessageButtonLabel
    } = useComponentDefaultProps('EmailServiceConfiguration', EmailServiceConfigurationFormDefaultProps, props);

    const formAPI = useEntityForm({ entity: entity, initialFormMode, getDataOnInit: getDataOnInit!, onSaved, onCancel });

    const { formMode, status, form } = formAPI;

    const theme = useMantineTheme();

    const minFormHeight = '75vh';

    return (
        <EntityForm formAPI={formAPI}>
            <Stack>
                <Card bg={theme.colorScheme === 'dark' ? theme.colors.dark[9] : undefined} withBorder={theme.colorScheme === 'dark' ? false : true} mih="65vh">
                    <Tabs defaultValue={TN.settings} mih={minFormHeight}>
                        <Tabs.List mb="xs">
                            <Tabs.Tab value={TN.settings} icon={settingsTABIcon} >{settingsTABLabel}</Tabs.Tab>
                            <Tabs.Tab value={TN.recovery} icon={recoveryTABIcon} >{recoveryTABLabel}</Tabs.Tab>
                        </Tabs.List>
                        <Tabs.Panel value={TN.settings}>
                            <Stack>
                                <TextField entityForm={formAPI} column={entity.def.columns.vc_smtp_host} />
                                <NumberField entityForm={formAPI} column={entity.def.columns.i_smtp_port} maw="20rem" />
                                <TextField entityForm={formAPI} column={entity.def.columns.vc_user_name} maw="20rem" />
                                <PasswordField entityForm={formAPI} column={entity.def.columns.vc_password} maw="20rem" />
                                <CheckboxField entityForm={formAPI} column={entity.def.columns.bt_use_ssl} />
                                <Divider />
                                <TextField entityForm={formAPI} column={entity.def.columns.vc_default_sender_email} />
                                <TextField entityForm={formAPI} column={entity.def.columns.vc_default_sender_name} />
                            </Stack>
                        </Tabs.Panel>
                        <Tabs.Panel value={TN.recovery}>
                            <Stack>
                                <TextField entityForm={formAPI} column={entity.def.columns.vc_template_subject} />
                                <TextAreaField entityForm={formAPI} column={entity.def.columns.vc_template_body} minRows={10} maxRows={10} />
                                <Group>
                                    <Button variant="outline" onClick={() => {
                                        form.setFieldValue(entity.def.columns.vc_template_subject.name, entity.def.columns.vc_template_subject.defaultValue);
                                        form.setFieldValue(entity.def.columns.vc_template_body.name, entity.def.columns.vc_template_body.defaultValue);
                                    }}>{setDefaultMessageButtonLabel}</Button>
                                </Group>
                            </Stack>
                        </Tabs.Panel>
                    </Tabs>
                </Card>
            </Stack>
        </EntityForm>
    )
}
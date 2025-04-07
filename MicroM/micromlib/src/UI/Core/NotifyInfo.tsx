import { Notification, NotificationProps, useComponentDefaultProps, useMantineTheme } from "@mantine/core";

export const NotifyInfoDefaultProps: Partial<NotificationProps> = {
    title: "Info"
}

export function NotifyInfo(props: NotificationProps) {
    const { title } = useComponentDefaultProps('NotifyInfo', NotifyInfoDefaultProps, props);
    const theme = useMantineTheme();
    return (
        <Notification
            icon="𝒊"
            title={title}
            color={props.color ?? theme.colors.blue[5]}
            withCloseButton={props.withCloseButton ?? false}
            {...props}>
            {props.children}
        </Notification>
    );
}
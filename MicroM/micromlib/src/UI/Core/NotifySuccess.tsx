import { Notification, NotificationProps, useComponentDefaultProps } from "@mantine/core";
import { IconCheck } from "@tabler/icons-react";

export const NotifySuccessDefaultProps: Partial<NotificationProps> = {
    title: "Success",
    color: "green"
}

export function NotifySuccess(props: NotificationProps) {
    const { title, color } = useComponentDefaultProps('NotifySuccess', NotifySuccessDefaultProps, props);


    return (
        <Notification
            icon={<IconCheck size="1.1rem" />}
            title={title}
            color={color}
            withCloseButton={props.withCloseButton ?? false}
            {...props}>
            {props.children}
        </Notification>
    );
}
import { Notification, NotificationProps } from "@mantine/core";
import { IconX } from "@tabler/icons-react";

export function NotifyError(props: NotificationProps) {

    return (
        <Notification
            icon={<IconX size="1.1rem" />}
            title={props.title ?? 'Error'}
            color={props.color ?? 'red'}
            withCloseButton={props.withCloseButton ?? false}
            {...props}>
            {props.children}
        </Notification>
    );
}
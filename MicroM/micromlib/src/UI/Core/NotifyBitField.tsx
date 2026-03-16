import { MantineColor, Notification, NotificationProps, useProps } from "@mantine/core";
import { IconCheck, IconX } from "@tabler/icons-react";
import { ReactNode } from "react";
import { EntityColumn } from "../../Entity";

export interface NotifyBitFieldProps extends NotificationProps {
    column: EntityColumn<boolean>
    trueIcon?: ReactNode,
    falseIcon?: ReactNode,
    trueMessage: ReactNode,
    falseMessage: ReactNode,
    trueColor?: MantineColor,
    falseColor?: MantineColor
}

export const NotifyBitFieldDefaultProps: Partial<NotifyBitFieldProps> = {
    trueIcon: <IconCheck size="1.1rem" />,
    falseIcon: <IconX size="1.1rem" />,
    trueColor: "green",
    falseColor: "red"
}

export function NotifyBitField(props: NotifyBitFieldProps) {
    const { trueIcon, falseIcon, trueMessage, falseMessage, column, trueColor, falseColor, ...others } = useProps('NotifyBitField', NotifyBitFieldDefaultProps, props);

    return (
        <Notification
            {...others}
            icon={column.value ? trueIcon : falseIcon}
            color={column.value ? trueColor : falseColor}
            withCloseButton={props.withCloseButton ?? false}
        >
            {column.value ? trueMessage : falseMessage}
        </Notification>
    );
}

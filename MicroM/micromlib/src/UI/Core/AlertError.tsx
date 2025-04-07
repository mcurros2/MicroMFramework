import { Alert, AlertProps, useComponentDefaultProps } from "@mantine/core";
import { IconAlertCircle } from "@tabler/icons-react";

export interface AlertErrorProps extends Omit<AlertProps, 'icon'> {
    iconTooltip?: string,
}

export const AlertErrorDefaultProps: Partial<AlertErrorProps> = {
    title: "Error",
    color: "red",
}

export function AlertError(props: AlertErrorProps) {
    const { title, color, iconTooltip, ...rest } = useComponentDefaultProps('AlertError', AlertErrorDefaultProps, props);

    return (
        <Alert icon={<IconAlertCircle size="2rem" title={iconTooltip} />} title={title} color={color} {...rest}>
            {props.children}
    </Alert>
    );
}
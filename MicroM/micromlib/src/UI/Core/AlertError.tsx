import { Alert, AlertProps, useProps } from "@mantine/core";
import { IconAlertCircle } from "@tabler/icons-react";

export interface AlertErrorProps extends Omit<AlertProps, 'icon'> {
    iconTooltip?: string,
}

export const AlertErrorDefaultProps: Partial<AlertErrorProps> = {
    title: "Error",
    color: "red",
}

export function AlertError(props: AlertErrorProps) {
    const { title, color, iconTooltip, ...rest } = useProps('AlertError', AlertErrorDefaultProps, props);

    return (
        <Alert icon={<IconAlertCircle size="2rem" title={iconTooltip} />} title={title} color={color} {...rest}>
            {props.children}
    </Alert>
    );
}

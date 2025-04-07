import { Alert, AlertProps, useComponentDefaultProps } from "@mantine/core";
import { IconCircleCheck } from "@tabler/icons-react";

export const AlertSuccessDefaultProps: Partial<AlertProps> = {
    title: "Success",
    color: "green"
}
export function AlertSuccess(props: AlertProps) {
    const { title, color } = useComponentDefaultProps('AlertSuccess', AlertSuccessDefaultProps, props);

    return (
        <Alert icon={<IconCircleCheck size="2rem" />} title={title} color={color} {...props} >
            {props.children}
        </Alert>
    );
}
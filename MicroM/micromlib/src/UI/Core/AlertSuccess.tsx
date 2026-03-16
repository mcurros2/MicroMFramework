import { Alert, AlertProps, useProps } from "@mantine/core";
import { IconCircleCheck } from "@tabler/icons-react";

export const AlertSuccessDefaultProps: Partial<AlertProps> = {
    title: "Success",
    color: "green"
}
export function AlertSuccess(props: AlertProps) {
    const { title, color } = useProps('AlertSuccess', AlertSuccessDefaultProps, props);

    return (
        <Alert icon={<IconCircleCheck size="2rem" />} title={title} color={color} {...props} >
            {props.children}
        </Alert>
    );
}

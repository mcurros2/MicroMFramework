import { AlertProps, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { Alert } from "@mantine/core";
import { IconInfoCircle } from "@tabler/icons-react";

export const AlertInfoDefaultProps: Partial<AlertProps> = {
    title: "Info"
}
export function AlertInfo(props: AlertProps) {
    const { title } = useComponentDefaultProps('AlertInfo', AlertInfoDefaultProps, props);

    const theme = useMantineTheme();

    return (
        <Alert icon={<IconInfoCircle size="2rem" />} title={title} color={theme.colors.blue[5]} {...props} >
            {props.children}
        </Alert>
    );
}
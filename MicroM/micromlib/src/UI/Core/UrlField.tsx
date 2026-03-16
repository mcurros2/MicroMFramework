import { useProps } from "@mantine/core";
import { IconWorld } from "@tabler/icons-react";
import { TextField, TextFieldProps } from "./TextField";


export interface UrlFieldProps extends Omit<TextFieldProps, 'validate'> {
    invalidMessage?: string
}

export const UrlFieldDefaultProps: Partial<UrlFieldProps> = {
    invalidMessage: "Invalid URL",
    requiredMessage: "A value is required"
}
export function UrlField(props: UrlFieldProps) {
    const { invalidMessage } = useProps('UrlField', UrlFieldDefaultProps, props);

    return (
        <TextField
            {...props}
            validate={{ url: { message: invalidMessage } }}
            leftSection={<IconWorld size="1rem" />}
        />
    )

}

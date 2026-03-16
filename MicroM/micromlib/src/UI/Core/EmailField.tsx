import { useProps } from "@mantine/core";
import { IconAt } from "@tabler/icons-react";
import { TextField, TextFieldProps } from "./TextField";

export interface EmailFieldProps extends Omit<TextFieldProps, 'validate'> {
    invalidMessage?: string
}

export const EmailFieldDefaultProps: Partial<EmailFieldProps> = {
    invalidMessage: "Enter a valid Email"
}

export function EmailField(props: EmailFieldProps) {
    const { invalidMessage } = useProps('EmailField', EmailFieldDefaultProps, props);
    return (
        <TextField
            {...props}
            validate={{ email: { message: invalidMessage } }}
            leftSection={<IconAt size="1rem" />}
        />
    )

}

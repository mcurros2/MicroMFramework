import { IconAt } from "@tabler/icons-react";
import { TextField, TextFieldProps } from "./TextField";
import { useComponentDefaultProps } from "@mantine/core";

export interface EmailFieldProps extends Omit<TextFieldProps, 'validate'> {
    invalidMessage?: string
}

export const EmailFieldDefaultProps: Partial<EmailFieldProps> = {
    invalidMessage: "Enter a valid Email"
}

export function EmailField(props: EmailFieldProps) {
    const { invalidMessage } = useComponentDefaultProps('EmailField', EmailFieldDefaultProps, props);
    return (
        <TextField
            {...props}
            validate={{ email: { message: invalidMessage } }}
            icon={<IconAt size="1rem" />}
        />
    )

}
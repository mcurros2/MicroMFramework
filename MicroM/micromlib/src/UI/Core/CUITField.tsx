import { useProps } from "@mantine/core";
import { IconId } from "@tabler/icons-react";
import { TextField, TextFieldProps } from "./TextField";


export interface CUITFieldProps extends TextFieldProps {
    invalidMessage?: string
}

export const CUITFieldDefaultProps: Partial<CUITFieldProps> = {
    invalidMessage: "Enter a valid CUIT/CUIL nummber"
}

export function CUITField(props: CUITFieldProps) {
    const { invalidMessage } = useProps('CUITField', CUITFieldDefaultProps, props);

    return (
        <TextField
            {...props}
            validate={{ cuit: { message: invalidMessage } }}
            leftSection={<IconId size="1rem" />}
        />
    )
}

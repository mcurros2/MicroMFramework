import { ActionIcon, Group, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconBrandWhatsapp, IconPhoneOutgoing } from "@tabler/icons-react";
import { useEffect, useRef } from "react";
import { TextField, TextFieldProps } from "./TextField";
import { Value } from "../../client";

export interface PhoneFieldProps extends Omit<TextFieldProps, 'validate'> {
    invalidMessage?: string,
    showCallIcon?: boolean,
    showWhatsappIcon?: boolean
}

export const PhoneFieldDefaultProps: Partial<PhoneFieldProps> = {
    invalidMessage: "Enter a valid Phone number. Only an optional + sign and numbers allowed.",
    placeholder: "+82113214576",
    showCallIcon: true,
    showWhatsappIcon: true
}

export function PhoneField(props: PhoneFieldProps) {
    const { invalidMessage, entityForm, column, showWhatsappIcon, showCallIcon, ...others } = useComponentDefaultProps('PhoneField', PhoneFieldDefaultProps, props);

    const theme = useMantineTheme();

    const { form } = entityForm;

    const callHref = useRef({});
    const whatsappHref = useRef({});
    const lastValidValue = useRef<Value>(null);

    useEffect(() => {
        if (lastValidValue.current !== null && form.values[column.name] === lastValidValue.current) return;
        if (form.isValid(column.name)) {
            callHref.current = { href: `tel:${entityForm.form.values[column.name]}`, target: '_blank' };
            whatsappHref.current = { href: `whatsapp://send?phone=${entityForm.form.values[column.name]}`, target: '_blank' };
            lastValidValue.current = entityForm.form.values[column.name];
        }
        else {
            callHref.current = {};
            whatsappHref.current = {};
        }
    }, [column.name, entityForm.form.values, form]);

    return (
        <TextField
            {...others}
            entityForm={entityForm}
            column={column}
            validate={{ phone: { message: invalidMessage } }}
            rightSection={(showCallIcon || showWhatsappIcon) &&
                <Group spacing="xs">
                    {showCallIcon && <ActionIcon component="a" {...callHref.current} rel="noopener noreferrer" color={theme.primaryColor} variant="light"><IconPhoneOutgoing size="1rem" /></ActionIcon>}
                    {showWhatsappIcon && <ActionIcon component="a" {...whatsappHref.current} rel="noopener noreferrer" color={theme.primaryColor} variant="light"><IconBrandWhatsapp size="1rem" /></ActionIcon>}
                </Group>
            }
            rightSectionWidth={showCallIcon && showWhatsappIcon ? "5rem" : "2.5rem"}
        />
    )

}
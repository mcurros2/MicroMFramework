import { ActionIcon, ActionIconProps, MantineColor, useComponentDefaultProps } from "@mantine/core";
import { useClipboard } from "@mantine/hooks";
import { IconCheck, IconCopy } from "@tabler/icons-react";
import { useRef } from "react";

export interface CopyToClipboardProps extends ActionIconProps {
    valueToCopy: unknown,
    timeout?: number,
    copiedColor?: MantineColor,
    iconSize?: string | number
}

const defaultProps: Partial<CopyToClipboardProps> = {
    timeout: 500,
    copiedColor: 'green',
    variant: 'filled',
    iconSize: '1.1rem'
}

export function CopyToClipboard(props: CopyToClipboardProps) {
    const {
        valueToCopy, timeout, copiedColor, iconSize
    } = useComponentDefaultProps('CopyToClipboard', defaultProps, props);

    const { copied, copy } = useClipboard({ timeout: timeout });
    const originalColor = useRef(props.color);

    return (
        <ActionIcon color={copied ? copiedColor : originalColor.current} onClick={() => copy(valueToCopy)} {...props} >
            {copied ? <IconCheck size={iconSize} /> : <IconCopy size={iconSize} />}
        </ActionIcon>
    )
}
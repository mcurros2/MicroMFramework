import { ActionIcon, Badge, BadgeProps } from "@mantine/core";
import { IconX } from "@tabler/icons-react";

export interface RegionSelectorItemProps extends Omit<BadgeProps, 'rightSection'> {
    onCloseClick?: () => void,
}

export function RegionSelectorItem({ onCloseClick, children, ...rest }: RegionSelectorItemProps) {

    const removeButton = (
        <ActionIcon size="xs" radius="xl" variant="transparent" onClick={() => { if (onCloseClick) onCloseClick() }}>
            <IconX size="0.75rem" />
        </ActionIcon>
    )

    return (
        <Badge size="md" {...rest} rightSection={onCloseClick && removeButton} styles={{ root: {width: 'fit-content'} } }>
            {children}
        </Badge>
    )
}
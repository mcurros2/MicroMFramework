import { px, rem } from '@mantine/core';
import { useViewportSize } from '@mantine/hooks';
import { useEffect, useState } from 'react';
export function useResponsiveWidth(maxWidthRem: string, paddingRem: string): string {
    const viewportSize = useViewportSize();
    const [width, setWidth] = useState<string>(maxWidthRem);

    useEffect(() => {
        const desiredWidth = Number(px(maxWidthRem));
        const totalPadding = Number(px(paddingRem)) * 2;
        const availableWidth = viewportSize.width - totalPadding;

        if (availableWidth < desiredWidth) {
            setWidth(rem(availableWidth));
        } else {
            setWidth(maxWidthRem);
        }
    }, [maxWidthRem, paddingRem, viewportSize.width]);

    return width;
}

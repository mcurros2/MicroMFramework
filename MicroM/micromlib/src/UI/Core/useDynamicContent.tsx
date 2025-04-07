import { Skeleton } from "@mantine/core";
import { ReactNode, useEffect, useState } from "react";

export interface UseDynamicContentProps {
    contentState: ReturnType<typeof useState<ReactNode>>,
    dynamicContent: Promise<ReactNode>,
    loading?: ReactNode
}

const defaultProps: Partial<UseDynamicContentProps> = {
    loading: <Skeleton />
}

export function useDynamicContent(props: UseDynamicContentProps) {
    const { dynamicContent, loading, contentState } = { ...defaultProps, ...props };
    const [, setContent] = contentState;

    useEffect(() => {
        let mounted = true;
        setContent(loading);
        dynamicContent.then(ImportedContent => {
            if (mounted) {
                setContent(ImportedContent);
            }
        });

        return () => {
            mounted = false;
        };
    }, [dynamicContent, loading, setContent]);

}
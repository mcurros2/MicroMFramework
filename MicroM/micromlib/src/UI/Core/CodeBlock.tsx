import { useComponentDefaultProps } from "@mantine/core";
import { Prism, PrismProps } from "@mantine/prism";

export interface CodeBlockProps extends Omit<PrismProps, 'children'> {
    codeText: string,
}

export const CodeBlockDefaultProps: Partial<CodeBlockProps> = {
}

export function CodeBlock(props: CodeBlockProps) {
    const {
        codeText
    } = useComponentDefaultProps('CodeBlock', CodeBlockDefaultProps, props);
    return (
        <Prism {...props}>
            {codeText ?? ''}
        </Prism>
    )
}
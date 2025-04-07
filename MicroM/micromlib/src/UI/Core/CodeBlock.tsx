import { useComponentDefaultProps } from "@mantine/core";
import { Prism, PrismProps } from "@mantine/prism";

export interface CodeBlockProps extends Omit<PrismProps, 'children'> {
    codeText: string,
}

const defaultProps: Partial<CodeBlockProps> = {

}
export function CodeBlock(props: CodeBlockProps) {
    const {
        codeText, language
    } = useComponentDefaultProps('CodeBlock', defaultProps, props);
    return (
        <Prism styles={{ scrollArea: { flexGrow: 1, minHeight: '60vh'} }} mih="60vh" mah="60vh" sx={{ display: "flex" }} language={language}>
            {codeText ?? ''}
        </Prism>
    )
}
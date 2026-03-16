import { useProps } from "@mantine/core";
import { CodeHighlight, CodeHighlightAdapterProvider, CodeHighlightProps, createHighlightJsAdapter } from "@mantine/code-highlight";
import hljs from "highlight.js/lib/core";
import javascript from "highlight.js/lib/languages/javascript";
import json from "highlight.js/lib/languages/json";
import sql from "highlight.js/lib/languages/sql";
import typescript from "highlight.js/lib/languages/typescript";
import "@mantine/code-highlight/styles.css";
import "highlight.js/styles/github.css";

hljs.registerLanguage("javascript", javascript);
hljs.registerLanguage("js", javascript);
hljs.registerLanguage("json", json);
hljs.registerLanguage("sql", sql);
hljs.registerLanguage("typescript", typescript);
hljs.registerLanguage("ts", typescript);

const highlightJsAdapter = createHighlightJsAdapter(hljs);

export interface CodeBlockProps extends Omit<CodeHighlightProps, 'code'> {
    codeText: string,
}

const defaultProps: Partial<CodeBlockProps> = {

}
export function CodeBlock(props: CodeBlockProps) {
    const {
        codeText, language, ...others
    } = useProps('CodeBlock', defaultProps, props);

    return (
        <CodeHighlightAdapterProvider adapter={highlightJsAdapter}>
            <CodeHighlight
                {...others}
                code={codeText ?? ''}
                language={language || 'plaintext'}
                style={{ minHeight: '60vh', maxHeight: '60vh', display: 'flex', flexDirection: 'column' }}
            />
        </CodeHighlightAdapterProvider>
    )
}

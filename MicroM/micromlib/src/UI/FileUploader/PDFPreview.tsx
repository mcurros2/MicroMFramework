import { Button, Group, Stack, Text, useComponentDefaultProps } from "@mantine/core";
import { FilePreviewProps } from "./ImagePreview";


export const PDFPreviewDefaultProps: Partial<FilePreviewProps> = {
    filePreviewError: 'PDF cannot be displayed, download the file to view it.',
}

export function PDFPreview(props: FilePreviewProps) {
    const { documentURL, onClose, closeText, filePreviewError } = useComponentDefaultProps('PDFPreview', PDFPreviewDefaultProps, props);


    return (
        <Stack>
            <div style={{ width: '100%', height: '70vh' }}>
                <object data={documentURL} type="application/pdf" width="100%" height="100%">
                    <Text size="sm" color="dimmed">{filePreviewError}</Text>
                </object>
            </div>
            <Group position="right">
                <Button onClick={onClose}>{closeText}</Button>
            </Group>
        </Stack>
    )
}
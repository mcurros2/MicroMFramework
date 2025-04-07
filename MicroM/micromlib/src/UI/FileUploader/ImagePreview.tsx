import { Button, Group, Stack, Image } from "@mantine/core"

export interface FilePreviewProps {
    documentURL: string,
    onClose: () => void,
    closeText: string,
    filePreviewError?: string
}

export function ImagePreview({ documentURL, onClose, closeText }: FilePreviewProps) {
    return (
        <Stack>
            <Image height="70vh" fit="contain" src={documentURL} />
            <Group position="right">
                <Button onClick={onClose}>{closeText}</Button>
            </Group>
        </Stack>
    )
}

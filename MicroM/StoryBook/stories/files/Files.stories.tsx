import { Box, Button, Code, Group, Image, Stack, Switch, Text } from "@mantine/core";
import {
    AvatarUploader, EntityForm, FileUploader, FilesUploadForm, ImageEditor, ImportDataPanel, resolveImageProcessingOptions, useAvatarUploader,
    useEntityForm, useFileUpload, WebcamCapture
} from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useEffect, useMemo, useState } from "react";
import { Customers, Documents } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";

function useDocuments() {
    return useMemo(() => new Documents(createMockClient({ latency: 250 })), []);
}

// A generated image so ImageEditor has something to crop/rotate without bundling binary assets.
function useSampleImage() {
    const [file, setFile] = useState<File>();
    useEffect(() => {
        const canvas = document.createElement("canvas");
        canvas.width = 800;
        canvas.height = 500;
        const ctx = canvas.getContext("2d")!;
        const g = ctx.createLinearGradient(0, 0, 800, 500);
        g.addColorStop(0, "#1f86a8");
        g.addColorStop(1, "#7b2cbf");
        ctx.fillStyle = g;
        ctx.fillRect(0, 0, 800, 500);
        ctx.fillStyle = "#fff";
        ctx.font = "bold 64px sans-serif";
        ctx.fillText("MicroM", 260, 270);
        canvas.toBlob(b => b && setFile(new File([b], "sample.png", { type: "image/png" })), "image/png");
    }, []);
    return file;
}

const meta = {
    title: "MicroM/Files",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

function FilesUploadFormStory() {
    const entity = useDocuments();
    const [editor, setEditor] = useState(true);
    return (
        <Stack>
            <Switch label="Image editor" checked={editor} onChange={e => setEditor(e.currentTarget.checked)} />
            <FilesUploadForm key={String(editor)} client={entity.API.client} fileProcessColumn={entity.def.columns.c_fileprocess_id} maxFilesCount={3} editor={editor} />
            <Text size="xs" color="dimmed">Uploads are kept in memory by MockMicroMClient (object URLs).</Text>
        </Stack>
    );
}

export const UploadForm: Story = { name: "FilesUploadForm", render: () => <FilesUploadFormStory /> };

function FileUploaderStory() {
    const entity = useDocuments();
    const uploadAPI = useFileUpload({ client: entity.API.client, fileProcessColumn: entity.def.columns.c_fileprocess_id, maxFilesCount: 5, editor: true });
    return <FileUploader uploadAPI={uploadAPI} accept={["image/*", "application/pdf"]} />;
}

export const Uploader: Story = { name: "FileUploader (useFileUpload)", render: () => <FileUploaderStory /> };

function AvatarStory() {
    const entity = useDocuments();
    const entityForm = useEntityForm({ entity, initialFormMode: "add", getDataOnInit: false });
    const avatarAPI = useAvatarUploader({
        client: entity.API.client,
        fileProcessColumn: entity.def.columns.c_fileprocess_id,
        fileGUIDColumn: entity.def.columns.vc_fileguid,
        parentFormAPI: entityForm,
        editor: true,
        imageProcessing: { crop: true, manualRotation: true, resize: true, compression: true },
    });
    return (
        <EntityForm formAPI={entityForm} showOK={false} showCancel={false}>
            <Group><AvatarUploader API={avatarAPI} size="xl" /></Group>
        </EntityForm>
    );
}

export const Avatar: Story = { name: "AvatarUploader", render: () => <AvatarStory /> };

function ImageEditorStory() {
    const file = useSampleImage();
    const [result, setResult] = useState<string>();
    const options = useMemo(() => resolveImageProcessingOptions({ crop: true, manualRotation: true }), []);
    if (!file) return <Text size="sm">Preparing sample image...</Text>;
    return (
        <Stack>
            <Box maw="48rem">
                <ImageEditor sourceFile={file} options={options} camera={false}
                    onSave={f => setResult(URL.createObjectURL(f))} onCancel={() => setResult(undefined)} />
            </Box>
            {result && <><Text size="sm">Saved result:</Text><Image src={result} maw="20rem" radius="sm" /></>}
        </Stack>
    );
}

export const Editor: Story = { name: "ImageEditor", render: () => <ImageEditorStory /> };

function WebcamStory() {
    const [shot, setShot] = useState<string>();
    const [open, setOpen] = useState(true);
    return (
        <Stack maw="40rem">
            {open
                ? <WebcamCapture onCapture={f => { setShot(URL.createObjectURL(f)); setOpen(false); }} onCancel={() => setOpen(false)} />
                : <Group><Button onClick={() => setOpen(true)}>Open camera again</Button></Group>}
            {shot && <Image src={shot} maw="20rem" radius="sm" />}
            <Text size="xs" color="dimmed">Requires camera permission in the browser.</Text>
        </Stack>
    );
}

export const Webcam: Story = { name: "WebcamCapture", render: () => <WebcamStory /> };

function ImportStory() {
    const client = useMemo(() => createMockClient(), []);
    return (
        <Stack>
            <Text size="sm">
                <Code>Import</Code> opens the 4-step wizard (instructions, upload a .csv/.xlsx, column mapping, summary). The mock returns 2 imported rows and 1 error.
            </Text>
            <Box h="60vh">
                <ImportDataPanel client={client} selectionMode="multi" formMode="edit"
                    entityConstructor={c => { const e = new Customers(c); return { entity: e, view: e.def.views.cus_brwStandard.name }; }} />
            </Box>
        </Stack>
    );
}

export const Import: Story = { name: "ImportDataPanel", render: () => <ImportStory /> };

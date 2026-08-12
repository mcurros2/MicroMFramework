import { Group, Select, Stack, Switch } from "@mantine/core";
import { MicroMClient } from "client";
import { useState } from "react";
import { AvatarUploader, EntityForm, useAvatarUploader, useEntityForm } from "UI";
import { AvatarUploaderTestEntity } from "./AvatarUploaderTestEntity";

export function AvatarUploaderTest() {
    const [client] = useState(() => new MicroMClient({ app_id: "", api_url: "" }));
    const [entity] = useState(() => new AvatarUploaderTestEntity(client));
    const [exifOrientation, setExifOrientation] = useState(true);
    const [editor, setEditor] = useState(true);
    const [crop, setCrop] = useState(true);
    const [resize, setResize] = useState(true);
    const [manualRotation, setManualRotation] = useState(true);
    const [compression, setCompression] = useState(true);
    const [outputFormat, setOutputFormat] = useState<string>();

    const entityForm = useEntityForm({ entity, initialFormMode: "add", getDataOnInit: false });

    const avatarAPI = useAvatarUploader({
        client: entity.API.client,
        fileProcessColumn: entity.def.columns.c_fileprocess_id,
        fileGUIDColumn: entity.def.columns.vc_fileguid,
        initialImageURL: "https://i.pravatar.cc/64?u=69",
        parentFormAPI: entityForm,
        editor,
        imageProcessing: {
            exifOrientation,
            crop,
            manualRotation,
            resize,
            compression,
            outputFormat
        }
    });

    return <EntityForm formAPI={entityForm}>
        <Stack>
            <Group>
                <Switch label="Editor" checked={editor} onChange={event => setEditor(event.currentTarget.checked)} />
                <Switch label="EXIF orientation" checked={exifOrientation} onChange={event => setExifOrientation(event.currentTarget.checked)} />
                <Switch label="Crop" checked={crop} onChange={event => setCrop(event.currentTarget.checked)} />
                <Switch label="Resize" checked={resize} onChange={event => setResize(event.currentTarget.checked)} />
                <Switch label="Manual rotation" checked={manualRotation} onChange={event => setManualRotation(event.currentTarget.checked)} />
                <Switch label="Compression" checked={compression} onChange={event => setCompression(event.currentTarget.checked)} />
                <Select
                    label="Output format"
                    clearable
                    placeholder="Preserve original"
                    value={outputFormat}
                    data={[
                        { value: 'image/jpeg', label: 'JPEG' },
                        { value: 'image/png', label: 'PNG' },
                        { value: 'image/webp', label: 'WebP' }
                    ]}
                    onChange={value => setOutputFormat(value ?? undefined)}
                />
            </Group>
            <AvatarUploader API={avatarAPI} />
        </Stack>
    </EntityForm>;
}

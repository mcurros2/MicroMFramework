import "react-advanced-cropper/dist/style.css";
import "./ImageEditor.scss";
import { Alert, Button, Group, Stack, useComponentDefaultProps } from "@mantine/core";
import { IconCheck, IconRotateClockwise, IconX } from "@tabler/icons-react";
import { useCallback, useRef, useState } from "react";
import { Cropper, CropperRef } from "react-advanced-cropper";
import { canvasToProcessedFile, getCanvasResizeOptions, ResolvedBrowserImageProcessingOptions } from "./imageProcessing";

export interface ImageEditorProps {
    src: string,
    sourceFile: File,
    options: ResolvedBrowserImageProcessingOptions,
    onSave: (file: File) => Promise<void> | void,
    onCancel: () => Promise<void> | void,
    saveLabel?: string,
    cancelLabel?: string,
    rotateClockwiseLabel?: string,
    rotateCounterClockwiseLabel?: string,
}

export const ImageEditorDefaultProps: Partial<ImageEditorProps> = {
    saveLabel: 'Save',
    cancelLabel: 'Cancel',
    rotateClockwiseLabel: 'Rotate clockwise',
    rotateCounterClockwiseLabel: 'Rotate counter-clockwise'
}

export const ImageEditor = (props: ImageEditorProps) => {
    const {
        src, sourceFile, options, onSave, onCancel, saveLabel, cancelLabel, rotateClockwiseLabel,
        rotateCounterClockwiseLabel
    } = useComponentDefaultProps('ImageEditor', ImageEditorDefaultProps, props);

    const cropperRef = useRef<CropperRef>(null);
    const [ready, setReady] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string>();
    const cropEnabled = options.crop !== false;
    const rotation = options.manualRotation;

    const rotate = useCallback((direction: 1 | -1) => {
        if (cropperRef.current && rotation !== false) {
            cropperRef.current.rotateImage(rotation.stepDegrees * direction);
        }
    }, [rotation]);

    const save = useCallback(async () => {
        const cropper = cropperRef.current;
        if (!cropper) return;

        setSaving(true);
        setError(undefined);
        try {
            const canvas = cropper.getCanvas(getCanvasResizeOptions(options));
            if (!canvas) throw new Error('The browser could not create the edited image.');
            const file = await canvasToProcessedFile(canvas, sourceFile, options);
            await onSave(file);
        }
        catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        }
        finally {
            setSaving(false);
        }
    }, [onSave, options, sourceFile]);

    return (
        <Stack className="image-editor" spacing="sm">
            <Cropper
                className="image-editor__cropper"
                src={src}
                ref={cropperRef}
                checkOrientation={options.exifOrientation}
                defaultSize={cropEnabled ? undefined : state => ({
                    width: state.imageSize.width,
                    height: state.imageSize.height
                })}
                stencilProps={{
                    aspectRatio: cropEnabled ? options.crop && options.crop.aspectRatio : undefined,
                    movable: cropEnabled,
                    resizable: cropEnabled,
                    handlers: cropEnabled,
                    lines: cropEnabled,
                    grid: cropEnabled
                }}
                backgroundWrapperProps={{
                    moveImage: cropEnabled,
                    scaleImage: cropEnabled
                }}
                onReady={() => setReady(true)}
                onError={() => setError('The browser could not decode the image.')}
            />

            {error && <Alert color="red">{error}</Alert>}

            <Group position="apart">
                <Group spacing="xs">
                    {rotation !== false && <>
                        <Button
                            variant="light"
                            title={rotateCounterClockwiseLabel}
                            aria-label={rotateCounterClockwiseLabel}
                            onClick={() => rotate(-1)}
                            disabled={!ready || saving}
                            leftIcon={<IconRotateClockwise size="1rem" style={{ transform: 'scaleX(-1)' }} />}
                        >
                            {rotateCounterClockwiseLabel}
                        </Button>
                        <Button
                            variant="light"
                            title={rotateClockwiseLabel}
                            aria-label={rotateClockwiseLabel}
                            onClick={() => rotate(1)}
                            disabled={!ready || saving}
                            leftIcon={<IconRotateClockwise size="1rem" />}
                        >
                            {rotateClockwiseLabel}
                        </Button>
                    </>}
                </Group>
                <Group spacing="xs">
                    <Button variant="light" color="gray" onClick={() => void onCancel()} disabled={saving} leftIcon={<IconX size="1rem" />}>
                        {cancelLabel}
                    </Button>
                    <Button onClick={() => void save()} loading={saving} disabled={!ready} leftIcon={<IconCheck size="1rem" />}>
                        {saveLabel}
                    </Button>
                </Group>
            </Group>
        </Stack>
    );
};

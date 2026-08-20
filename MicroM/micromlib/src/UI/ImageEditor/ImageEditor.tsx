import "react-easy-crop/react-easy-crop.css";
import "./ImageEditor.scss";
import { Alert, Button, Group, Stack, Text, useComponentDefaultProps } from "@mantine/core";
import { IconCamera, IconCheck, IconRotate2, IconRotateClockwise2, IconX } from "@tabler/icons-react";
import { useCallback, useEffect, useState } from "react";
import Cropper, { Area, Point } from "react-easy-crop";
import { WebcamCapture, WebcamCaptureProps } from "../WebcamCapture";
import { FullImagePreview } from "./FullImagePreview";
import { prepareEditorImage, renderEditedImage } from "./imageEditorProcessing";
import { canvasToProcessedFile, ResolvedBrowserImageProcessingOptions } from "./imageProcessing";

export type ImageEditorCameraConfiguration = Omit<WebcamCaptureProps, 'onCapture' | 'onCancel'>;

export interface ImageEditorProps {
    sourceFile?: File,
    options: ResolvedBrowserImageProcessingOptions,
    onSave: (file: File) => Promise<void> | void,
    onBeforeSave?: (file: File) => boolean | Promise<boolean>,
    onCancel: () => Promise<void> | void,
    initiallyDirty?: boolean,
    saveLabel?: string,
    cancelLabel?: string,
    rotateClockwiseLabel?: string,
    rotateCounterClockwiseLabel?: string,
    takePhotoLabel?: string,
    camera?: boolean | ImageEditorCameraConfiguration,
}

export const ImageEditorDefaultProps: Partial<ImageEditorProps> = {
    saveLabel: 'Save',
    cancelLabel: 'Cancel',
    rotateClockwiseLabel: 'Rotate right',
    rotateCounterClockwiseLabel: 'Rotate left',
    takePhotoLabel: 'Take photo',
    camera: true,
    initiallyDirty: true
}

export const ImageEditor = (props: ImageEditorProps) => {
    const {
        sourceFile, options, onSave, onBeforeSave, onCancel, initiallyDirty, saveLabel, cancelLabel,
        rotateClockwiseLabel, rotateCounterClockwiseLabel, takePhotoLabel, camera
    } = useComponentDefaultProps('ImageEditor', ImageEditorDefaultProps, props);

    const [editorSource, setEditorSource] = useState<File | undefined>(sourceFile);
    const [cameraMode, setCameraMode] = useState(!sourceFile && camera !== false);
    const [preparedImage, setPreparedImage] = useState<Awaited<ReturnType<typeof prepareEditorImage>>>();
    const [crop, setCrop] = useState<Point>({ x: 0, y: 0 });
    const [zoom, setZoom] = useState(1);
    const [rotationDegrees, setRotationDegrees] = useState(0);
    const [croppedAreaPixels, setCroppedAreaPixels] = useState<Area>();
    const [ready, setReady] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string>();

    const cropEnabled = options.crop !== false;
    const manualRotation = options.manualRotation;

    const dirty = !!editorSource && (
        initiallyDirty === true
        || editorSource !== sourceFile
        || crop.x !== 0
        || crop.y !== 0
        || zoom !== 1
        || rotationDegrees !== 0
    );

    useEffect(() => {
        setEditorSource(sourceFile);
        setCameraMode(!sourceFile && camera !== false);
    }, [camera, sourceFile]);

    useEffect(() => {
        let disposed = false;
        let prepared: Awaited<ReturnType<typeof prepareEditorImage>> | undefined;

        setPreparedImage(undefined);
        setCrop({ x: 0, y: 0 });
        setZoom(1);
        setRotationDegrees(0);
        setCroppedAreaPixels(undefined);
        setReady(false);
        setSaving(false);
        setError(undefined);

        if (!editorSource) return;

        void prepareEditorImage(editorSource, options.exifOrientation)
            .then(result => {
                prepared = result;
                if (disposed) {
                    URL.revokeObjectURL(result.previewURL);
                    return;
                }
                setPreparedImage(result);
                if (!cropEnabled) setReady(true);
            })
            .catch(e => {
                if (!disposed) setError(e instanceof Error ? e.message : String(e));
            });

        return () => {
            disposed = true;
            if (prepared) URL.revokeObjectURL(prepared.previewURL);
        };
    }, [cropEnabled, editorSource, options.exifOrientation]);

    const rotate = useCallback((direction: 1 | -1) => {
        if (manualRotation === false) return;

        if (cropEnabled) {
            setReady(false);
            setCroppedAreaPixels(undefined);
        }

        setRotationDegrees(current => ((current + manualRotation.stepDegrees * direction) % 360 + 360) % 360);
    }, [cropEnabled, manualRotation]);

    const save = useCallback(async () => {
        if (!dirty || !editorSource || !preparedImage || (cropEnabled && !croppedAreaPixels)) return;

        setSaving(true);
        setError(undefined);

        try {
            const canvas = renderEditedImage(
                preparedImage.canvas,
                cropEnabled ? croppedAreaPixels : undefined,
                rotationDegrees,
                options
            );

            const file = await canvasToProcessedFile(canvas, editorSource, options);

            if (onBeforeSave && !await onBeforeSave(file)) return;
            await onSave(file);
        }
        catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        }
        finally {
            setSaving(false);
        }
    }, [cropEnabled, croppedAreaPixels, dirty, editorSource, onBeforeSave, onSave, options, preparedImage, rotationDegrees]);

    const cameraProps = typeof camera === 'object' ? camera : {};

    return (
        <Stack className="image-editor" spacing="sm">
            <Group spacing="xs">
                {camera !== false &&
                    <Button
                        variant="light"
                        title={takePhotoLabel}
                        aria-label={takePhotoLabel}
                        onClick={() => setCameraMode(true)}
                        disabled={saving || cameraMode}
                        leftIcon={<IconCamera size="1rem" />}
                    >
                        {takePhotoLabel}
                    </Button>
                }
                {manualRotation !== false && !cameraMode &&
                    <>
                    <Button
                        variant="light"
                        title={rotateCounterClockwiseLabel}
                        aria-label={rotateCounterClockwiseLabel}
                        onClick={() => rotate(-1)}
                        disabled={!ready || saving}
                        leftIcon={<IconRotate2 size="1rem" />}
                    >
                        {rotateCounterClockwiseLabel}
                    </Button>
                    <Button
                        variant="light"
                        title={rotateClockwiseLabel}
                        aria-label={rotateClockwiseLabel}
                        onClick={() => rotate(1)}
                        disabled={!ready || saving}
                        leftIcon={<IconRotateClockwise2 size="1rem" />}
                    >
                        {rotateClockwiseLabel}
                    </Button>
                    </>
                }
            </Group>

            {cameraMode && camera !== false &&
                <WebcamCapture
                    {...cameraProps}
                    onCapture={file => {
                        setEditorSource(file);
                        setCameraMode(false);
                    }}
                    onCancel={() => setCameraMode(false)}
                />
            }

            {!cameraMode && preparedImage && cropEnabled && options.crop !== false &&
                <div className="image-editor__cropper">
                    <Cropper
                        image={preparedImage.previewURL}
                        crop={crop}
                        zoom={zoom}
                        rotation={rotationDegrees}
                        aspect={options.crop.aspectRatio}
                        showGrid
                        roundCropAreaPixels
                        onCropChange={setCrop}
                        onZoomChange={setZoom}
                        onCropComplete={(_, area) => {
                            setCroppedAreaPixels(area);
                            setReady(true);
                        }}
                        mediaProps={{
                            alt: editorSource?.name,
                            onError: () => setError('The browser could not decode the image.')
                        }}
                    />
                </div>
            }
            {!cameraMode && preparedImage && !cropEnabled && editorSource &&
                <FullImagePreview source={preparedImage.canvas} rotation={rotationDegrees} label={editorSource.name} />
            }

            {!cameraMode && !editorSource &&
                <div className="image-editor__empty">
                    <Text color="dimmed">{takePhotoLabel}</Text>
                </div>
            }

            {error && <Alert color="red">{error}</Alert>}

            {!cameraMode && <Group position="right">
                <Group spacing="xs">
                    <Button variant="light" onClick={() => void onCancel()} disabled={saving} leftIcon={<IconX size="1rem" />}>
                        {cancelLabel}
                    </Button>
                    <Button onClick={() => void save()} loading={saving} disabled={!dirty || !ready} leftIcon={<IconCheck size="1rem" />}>
                        {saveLabel}
                    </Button>
                </Group>
            </Group>}
        </Stack>
    );
};

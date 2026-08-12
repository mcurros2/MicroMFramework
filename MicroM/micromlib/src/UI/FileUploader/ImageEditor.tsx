import "react-easy-crop/react-easy-crop.css";
import "./ImageEditor.scss";
import { Alert, Button, Group, Stack, useComponentDefaultProps } from "@mantine/core";
import { IconCheck, IconRotate2, IconRotateClockwise2, IconX } from "@tabler/icons-react";
import { useCallback, useEffect, useRef, useState } from "react";
import Cropper, { Area, Point } from "react-easy-crop";
import { getRotatedSize, prepareEditorImage, renderEditedImage } from "./imageEditorProcessing";
import { canvasToProcessedFile, ResolvedBrowserImageProcessingOptions } from "./imageProcessing";

interface ImageEditorProps {
    sourceFile: File,
    options: ResolvedBrowserImageProcessingOptions,
    onSave: (file: File) => Promise<void> | void,
    onCancel: () => Promise<void> | void,
    saveLabel?: string,
    cancelLabel?: string,
    rotateClockwiseLabel?: string,
    rotateCounterClockwiseLabel?: string,
}

interface FullImagePreviewProps {
    source: HTMLCanvasElement,
    rotation: number,
    label: string,
}

const FullImagePreview = ({ source, rotation, label }: FullImagePreviewProps) => {
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const canvas = canvasRef.current;
        if (!canvas) return;

        const draw = () => {
            const { width, height } = canvas.getBoundingClientRect();
            if (width === 0 || height === 0) return;

            const pixelRatio = window.devicePixelRatio || 1;
            canvas.width = Math.max(1, Math.round(width * pixelRatio));
            canvas.height = Math.max(1, Math.round(height * pixelRatio));

            const context = canvas.getContext('2d');
            if (!context) return;

            const rotatedSize = getRotatedSize(source.width, source.height, rotation);
            const scale = Math.min(width / rotatedSize.width, height / rotatedSize.height);

            context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
            context.fillStyle = '#0f0e13';
            context.fillRect(0, 0, width, height);
            context.translate(width / 2, height / 2);
            context.rotate(rotation * Math.PI / 180);
            context.scale(scale, scale);
            context.drawImage(source, -source.width / 2, -source.height / 2);
        };

        draw();

        const observer = new ResizeObserver(draw);
        observer.observe(canvas);

        return () => observer.disconnect();
    }, [rotation, source]);

    return <canvas ref={canvasRef} className="image-editor__cropper" role="img" aria-label={label} />;
};

export const ImageEditorDefaultProps: Partial<ImageEditorProps> = {
    saveLabel: 'Save',
    cancelLabel: 'Cancel',
    rotateClockwiseLabel: 'Rotate clockwise',
    rotateCounterClockwiseLabel: 'Rotate counter-clockwise'
}

export const ImageEditor = (props: ImageEditorProps) => {
    const {
        sourceFile, options, onSave, onCancel, saveLabel, cancelLabel, rotateClockwiseLabel,
        rotateCounterClockwiseLabel
    } = useComponentDefaultProps('ImageEditor', ImageEditorDefaultProps, props);

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

    useEffect(() => {
        let disposed = false;
        let prepared: Awaited<ReturnType<typeof prepareEditorImage>> | undefined;

        setReady(false);
        setError(undefined);

        void prepareEditorImage(sourceFile, options.exifOrientation)
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
    }, [cropEnabled, options.exifOrientation, sourceFile]);

    const rotate = useCallback((direction: 1 | -1) => {
        if (manualRotation === false) return;

        if (cropEnabled) {
            setReady(false);
            setCroppedAreaPixels(undefined);
        }

        setRotationDegrees(current => (current + manualRotation.stepDegrees * direction) % 360);
    }, [cropEnabled, manualRotation]);

    const save = useCallback(async () => {
        if (!preparedImage || (cropEnabled && !croppedAreaPixels)) return;

        setSaving(true);
        setError(undefined);

        try {
            const canvas = renderEditedImage(
                preparedImage.canvas,
                cropEnabled ? croppedAreaPixels : undefined,
                rotationDegrees,
                options
            );
            const file = await canvasToProcessedFile(canvas, sourceFile, options);
            await onSave(file);
        }
        catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        }
        finally {
            setSaving(false);
        }
    }, [cropEnabled, croppedAreaPixels, onSave, options, preparedImage, rotationDegrees, sourceFile]);

    return (
        <Stack className="image-editor" spacing="sm">
            {manualRotation !== false &&
                <Group spacing="xs">
                    <Button
                        variant="light"
                        title={rotateCounterClockwiseLabel}
                        aria-label={rotateCounterClockwiseLabel}
                        onClick={() => rotate(-1)}
                        disabled={!ready || saving}
                        leftIcon={<IconRotate2 size="1rem" style={{ transform: 'scaleX(-1)' }} />}
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
                </Group>
            }
            {preparedImage && cropEnabled && options.crop !== false &&
                <div className="image-editor__cropper">
                    <Cropper
                        image={preparedImage.previewURL}
                        crop={crop}
                        zoom={zoom}
                        rotation={rotationDegrees}
                        aspect={options.crop.aspectRatio}
                        showGrid
                        roundCropAreaPixels
                        disableAutomaticStylesInjection
                        onCropChange={setCrop}
                        onZoomChange={setZoom}
                        onCropAreaChange={(_, area) => {
                            setCroppedAreaPixels(area);
                            setReady(true);
                        }}
                        mediaProps={{
                            alt: sourceFile.name,
                            onError: () => setError('The browser could not decode the image.')
                        }}
                    />
                </div>
            }
            {preparedImage && !cropEnabled &&
                <FullImagePreview source={preparedImage.canvas} rotation={rotationDegrees} label={sourceFile.name} />
            }

            {error && <Alert color="red">{error}</Alert>}

            <Group position="right">
                <Group spacing="xs">
                    <Button variant="light" onClick={() => void onCancel()} disabled={saving} leftIcon={<IconX size="1rem" />}>
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

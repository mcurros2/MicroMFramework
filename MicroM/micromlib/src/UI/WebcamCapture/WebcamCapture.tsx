import { Alert, Button, Group, Select, Stack, Text, useComponentDefaultProps } from "@mantine/core";
import { IconCamera, IconCheck, IconRefresh, IconVideo, IconX } from "@tabler/icons-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

export interface WebcamCaptureLabels {
    permissionMessage?: string,
    enableCameraLabel?: string,
    cameraLabel?: string,
    captureLabel?: string,
    retakeLabel?: string,
    usePhotoLabel?: string,
    cancelLabel?: string,
    unsupportedMessage?: string,
    insecureContextMessage?: string,
    permissionDeniedMessage?: string,
    noCameraMessage?: string,
    cameraUnavailableMessage?: string,
}

export interface WebcamCaptureProps {
    onCapture: (file: File) => Promise<void> | void,
    onCancel: () => Promise<void> | void,
    labels?: WebcamCaptureLabels,
    videoConstraints?: MediaTrackConstraints,
    fileName?: string,
    outputType?: 'image/jpeg' | 'image/png' | 'image/webp',
    outputQuality?: number,
}

const WebcamCaptureDefaultLabels: Required<WebcamCaptureLabels> = {
    permissionMessage: 'Camera permission is required to take a photo.',
    enableCameraLabel: 'Enable camera',
    cameraLabel: 'Camera',
    captureLabel: 'Take photo',
    retakeLabel: 'Retake',
    usePhotoLabel: 'Use photo',
    cancelLabel: 'Cancel',
    unsupportedMessage: 'This browser does not support camera capture.',
    insecureContextMessage: 'Camera access requires HTTPS or localhost.',
    permissionDeniedMessage: 'Camera access was denied. Allow camera access in the browser settings, then try again.',
    noCameraMessage: 'No camera was found.',
    cameraUnavailableMessage: 'The camera is unavailable or already in use.'
};

export const WebcamCaptureDefaultProps: Partial<WebcamCaptureProps> = {
    labels: WebcamCaptureDefaultLabels,
    videoConstraints: { facingMode: 'user' },
    fileName: 'camera-photo.jpg',
    outputType: 'image/jpeg',
    outputQuality: 0.92
};

type CapturedPhoto = { file: File, previewURL: string };

function stopStream(stream?: MediaStream) {
    stream?.getTracks().forEach(track => track.stop());
}

function errorMessage(error: unknown, labels: Required<WebcamCaptureLabels>) {
    if (!(error instanceof DOMException)) {
        return error instanceof Error ? error.message : String(error);
    }

    switch (error.name) {
        case 'NotAllowedError':
        case 'SecurityError':
            return labels.permissionDeniedMessage;
        case 'NotFoundError':
        case 'OverconstrainedError':
            return labels.noCameraMessage;
        case 'NotReadableError':
        case 'AbortError':
            return labels.cameraUnavailableMessage;
        default:
            return error.message || labels.cameraUnavailableMessage;
    }
}

function canvasToFile(canvas: HTMLCanvasElement, fileName: string, type: string, quality?: number) {
    return new Promise<File>((resolve, reject) => {
        canvas.toBlob(blob => {
            if (!blob) {
                reject(new Error('The browser could not encode the captured image.'));
                return;
            }
            resolve(new File([blob], fileName, { type: blob.type || type, lastModified: Date.now() }));
        }, type, quality);
    });
}

export function WebcamCapture(props: WebcamCaptureProps) {
    const {
        onCapture, onCancel, labels: suppliedLabels, videoConstraints, fileName,
        outputType, outputQuality
    } = useComponentDefaultProps('WebcamCapture', WebcamCaptureDefaultProps, props);

    const labels = useMemo(
        () => ({ ...WebcamCaptureDefaultLabels, ...suppliedLabels }),
        [suppliedLabels]
    );

    const videoRef = useRef<HTMLVideoElement>(null);
    const streamRef = useRef<MediaStream>();
    const capturedRef = useRef<CapturedPhoto>();
    const requestRef = useRef(0);
    const [permission, setPermission] = useState<PermissionState | 'unknown'>('unknown');
    const [devices, setDevices] = useState<MediaDeviceInfo[]>([]);
    const [selectedDeviceID, setSelectedDeviceID] = useState<string | null>(null);
    const [captured, setCaptured] = useState<CapturedPhoto>();
    const [starting, setStarting] = useState(false);
    const [streaming, setStreaming] = useState(false);
    const [error, setError] = useState<string>();

    capturedRef.current = captured;

    const releaseCamera = useCallback(() => {
        requestRef.current += 1;
        stopStream(streamRef.current);
        streamRef.current = undefined;
        setStreaming(false);
        if (videoRef.current) videoRef.current.srcObject = null;
    }, []);

    const clearCaptured = useCallback(() => {
        setCaptured(previous => {
            if (previous) URL.revokeObjectURL(previous.previewURL);
            return undefined;
        });
    }, []);

    const loadDevices = useCallback(async () => {
        const available = await navigator.mediaDevices.enumerateDevices();
        const cameras = available.filter(device => device.kind === 'videoinput');
        setDevices(cameras);
        return cameras;
    }, []);

    const startCamera = useCallback(async (deviceID?: string | null) => {
        setError(undefined);

        if (!window.isSecureContext) {
            setError(labels.insecureContextMessage);
            return;
        }
        if (!navigator.mediaDevices?.getUserMedia || !navigator.mediaDevices.enumerateDevices) {
            setError(labels.unsupportedMessage);
            return;
        }

        const requestID = requestRef.current + 1;
        requestRef.current = requestID;
        setStarting(true);
        setStreaming(false);
        stopStream(streamRef.current);
        streamRef.current = undefined;

        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: false,
                video: {
                    ...videoConstraints,
                    ...(deviceID ? { deviceId: { exact: deviceID } } : {})
                }
            });

            if (requestRef.current !== requestID) {
                stopStream(stream);
                return;
            }

            streamRef.current = stream;
            if (videoRef.current) {
                videoRef.current.srcObject = stream;
                await videoRef.current.play();
            }

            setStreaming(true);
            setPermission('granted');

            const cameras = await loadDevices();
            const activeDeviceID = stream.getVideoTracks()[0]?.getSettings().deviceId;

            setSelectedDeviceID(activeDeviceID ?? deviceID ?? cameras[0]?.deviceId ?? null);
        }
        catch (e: unknown) {
            if (requestRef.current === requestID) {
                stopStream(streamRef.current);
                streamRef.current = undefined;
                setStreaming(false);

                if (videoRef.current) videoRef.current.srcObject = null;

                if (e instanceof DOMException && (e.name === 'NotAllowedError' || e.name === 'SecurityError')) {
                    setPermission('denied');
                }

                setError(errorMessage(e, labels));
            }
        }
        finally {
            if (requestRef.current === requestID) setStarting(false);
        }
    }, [labels, loadDevices, videoConstraints]);

    useEffect(() => {
        let disposed = false;
        let permissionStatus: PermissionStatus | undefined;

        if (navigator.permissions?.query) {
            void navigator.permissions.query({ name: 'camera' as PermissionName })
                .then(status => {
                    if (disposed) return;
                    permissionStatus = status;
                    setPermission(status.state);
                    status.onchange = () => {
                        if (!disposed) setPermission(status.state);
                    };
                })
                .catch(() => { /* getUserMedia remains the authoritative permission check. */ });
        }

        return () => {
            disposed = true;
            if (permissionStatus) permissionStatus.onchange = null;
        };
    }, []);

    useEffect(() => () => {
        releaseCamera();
        if (capturedRef.current) URL.revokeObjectURL(capturedRef.current.previewURL);
    }, [releaseCamera]);

    const capture = useCallback(async () => {
        const video = videoRef.current;
        if (!video || video.videoWidth === 0 || video.videoHeight === 0) return;

        setError(undefined);
        try {
            const canvas = document.createElement('canvas');
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;

            const context = canvas.getContext('2d');

            if (!context) throw new Error('The browser could not create an image canvas.');
            context.drawImage(video, 0, 0, canvas.width, canvas.height);

            const file = await canvasToFile(canvas, fileName!, outputType!, outputQuality);

            clearCaptured();
            setCaptured({ file, previewURL: URL.createObjectURL(file) });
        }
        catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        }
    }, [clearCaptured, fileName, outputQuality, outputType]);

    const accept = useCallback(async () => {
        if (!captured) return;

        const file = captured.file;

        releaseCamera();
        clearCaptured();

        await onCapture(file);
    }, [captured, clearCaptured, onCapture, releaseCamera]);

    const cancel = useCallback(async () => {
        releaseCamera();
        clearCaptured();

        await onCancel();
    }, [clearCaptured, onCancel, releaseCamera]);

    const deviceOptions = devices.map((device, index) => ({
        value: device.deviceId,
        label: device.label || `${labels.cameraLabel} ${index + 1}`
    }));

    return (
        <Stack spacing="sm">
            {!streaming && !captured &&
                <Stack align="center" py="xl">
                    <IconVideo size="3rem" />
                    <Text align="center">{labels.permissionMessage}</Text>
                    {permission === 'denied' && <Text size="sm" color="dimmed" align="center">{labels.permissionDeniedMessage}</Text>}
                    <Button
                        leftIcon={<IconCamera size="1rem" />}
                        loading={starting}
                        onClick={() => void startCamera(selectedDeviceID)}
                    >
                        {labels.enableCameraLabel}
                    </Button>
                </Stack>
            }

            <video
                ref={videoRef}
                autoPlay
                muted
                playsInline
                hidden={!!captured || !streaming}
                style={{ width: '100%', maxHeight: '60vh', objectFit: 'cover', borderRadius: '1rem', background: '#0f0e13', display: (!!captured || !streaming) ? 'none' : 'inline-block' }}
            />

            {captured &&
                <img
                    src={captured.previewURL}
                    alt={captured.file.name}
                    style={{ width: '100%', maxHeight: '60vh', objectFit: 'cover', borderRadius: '1rem', background: '#0f0e13' }}
                />
            }

            {error && <Alert color="red">{error}</Alert>}

            <Group position="apart">
                <Group spacing="xs">
                    {deviceOptions.length > 1 && !captured &&
                        <Select
                            aria-label={labels.cameraLabel}
                            data={deviceOptions}
                            value={selectedDeviceID}
                            onChange={value => {
                                setSelectedDeviceID(value);
                                void startCamera(value);
                            }}
                        />
                    }
                </Group>
                <Group spacing="xs">
                    <Button variant="light" leftIcon={<IconX size="1rem" />} onClick={() => void cancel()}>
                        {labels.cancelLabel}
                    </Button>
                    {streaming && !captured &&
                        <Button leftIcon={<IconCamera size="1rem" />} onClick={() => void capture()}>
                            {labels.captureLabel}
                        </Button>
                    }
                    {captured &&
                        <>
                            <Button variant="light" leftIcon={<IconRefresh size="1rem" />} onClick={clearCaptured}>
                                {labels.retakeLabel}
                            </Button>
                            <Button leftIcon={<IconCheck size="1rem" />} onClick={() => void accept()}>
                                {labels.usePhotoLabel}
                            </Button>
                        </>
                    }
                </Group>
            </Group>
        </Stack>
    );
}

import mime from "mime";
import type { Area } from "react-easy-crop";
import { canvasToBlob, closeDecodedImage, getDecodedImageSize, loadDecodedImage } from "./imageCanvas";
import { ResolvedBrowserImageProcessingOptions } from "./imageProcessing";

export async function prepareEditorImage(file: File, respectExifOrientation: boolean) {
    const image = await loadDecodedImage(file, respectExifOrientation);
    try {
        const { width, height } = getDecodedImageSize(image);
        const canvas = document.createElement('canvas');

        canvas.width = width;
        canvas.height = height;

        const context = canvas.getContext('2d');
        if (!context) throw new Error('The browser could not create an image canvas.');
        context.drawImage(image, 0, 0);

        const preview = await canvasToBlob(canvas, mime.getType('png')!);

        return {
            canvas,
            previewURL: URL.createObjectURL(preview)
        };
    }
    finally {
        closeDecodedImage(image);
    }
}

export function getRotatedSize(width: number, height: number, rotation: number) {
    const radians = rotation * Math.PI / 180;

    return {
        width: Math.max(1, Math.round(Math.abs(Math.cos(radians) * width) + Math.abs(Math.sin(radians) * height))),
        height: Math.max(1, Math.round(Math.abs(Math.sin(radians) * width) + Math.abs(Math.cos(radians) * height)))
    };
}

function getCropArea(canvas: HTMLCanvasElement, crop?: Area) {
    if (!crop) {
        return { x: 0, y: 0, width: canvas.width, height: canvas.height };
    }

    const x = Math.max(0, Math.min(canvas.width - 1, Math.round(crop.x)));
    const y = Math.max(0, Math.min(canvas.height - 1, Math.round(crop.y)));

    return {
        x,
        y,
        width: Math.max(1, Math.min(canvas.width - x, Math.round(crop.width))),
        height: Math.max(1, Math.min(canvas.height - y, Math.round(crop.height)))
    };
}

export function renderEditedImage(source: HTMLCanvasElement, crop: Area | undefined, rotation: number, options: ResolvedBrowserImageProcessingOptions) {
    const rotatedSize = getRotatedSize(source.width, source.height, rotation);
    const rotatedCanvas = document.createElement('canvas');

    rotatedCanvas.width = rotatedSize.width;
    rotatedCanvas.height = rotatedSize.height;

    const rotatedContext = rotatedCanvas.getContext('2d');
    if (!rotatedContext) throw new Error('The browser could not create an image canvas.');

    rotatedContext.translate(rotatedCanvas.width / 2, rotatedCanvas.height / 2);
    rotatedContext.rotate(rotation * Math.PI / 180);
    rotatedContext.drawImage(source, -source.width / 2, -source.height / 2);

    const cropArea = getCropArea(rotatedCanvas, crop);
    const resize = options.resize;

    const requestedScale = resize === false
        ? 1
        : Math.min(resize.maxWidth / cropArea.width, resize.maxHeight / cropArea.height);

    const scale = resize === false || resize.allowUpscale
        ? requestedScale
        : Math.min(1, requestedScale);

    const output = document.createElement('canvas');
    output.width = Math.max(1, Math.round(cropArea.width * scale));
    output.height = Math.max(1, Math.round(cropArea.height * scale));

    const outputContext = output.getContext('2d');
    if (!outputContext) throw new Error('The browser could not create an image canvas.');

    outputContext.imageSmoothingEnabled = true;
    outputContext.imageSmoothingQuality = 'high';

    outputContext.drawImage(
        rotatedCanvas,
        cropArea.x,
        cropArea.y,
        cropArea.width,
        cropArea.height,
        0,
        0,
        output.width,
        output.height
    );

    return output;
}

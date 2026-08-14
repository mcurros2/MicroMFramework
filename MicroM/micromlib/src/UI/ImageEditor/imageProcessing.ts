import mime from "mime";
import { canvasToBlob, closeDecodedImage, getDecodedImageSize, loadDecodedImage } from "./imageCanvas";

export interface ImageCropOptions {
    aspectRatio?: number;
}

export interface ImageResizeOptions {
    maxWidth?: number;
    maxHeight?: number;
    allowUpscale?: boolean;
}

export interface ImageManualRotationOptions {
    stepDegrees?: 90 | -90;
}

export interface ImageCompressionOptions {
    thresholdBytes?: number;
    quality?: number;
    fallbackFormat?: string;
}

export interface BrowserImageProcessingOptions {
    exifOrientation?: boolean;
    crop?: boolean | ImageCropOptions;
    resize?: boolean | ImageResizeOptions;
    manualRotation?: boolean | ImageManualRotationOptions;
    compression?: boolean | ImageCompressionOptions;
    outputFormat?: string;
    jpegBackgroundColor?: string;
}

export interface ResolvedBrowserImageProcessingOptions {
    exifOrientation: boolean;
    crop: false | Required<ImageCropOptions>;
    resize: false | Required<ImageResizeOptions>;
    manualRotation: false | Required<ImageManualRotationOptions>;
    compression: false | Required<ImageCompressionOptions>;
    outputFormat?: string;
    jpegBackgroundColor: string;
}

export const BrowserImageProcessingDefaults: ResolvedBrowserImageProcessingOptions = {
    exifOrientation: true,
    crop: false,
    resize: false,
    manualRotation: false,
    compression: false,
    outputFormat: undefined,
    jpegBackgroundColor: '#ffffff'
};

const cropDefaults: Required<ImageCropOptions> = {
    aspectRatio: 1
};

const resizeDefaults: Required<ImageResizeOptions> = {
    maxWidth: 512,
    maxHeight: 512,
    allowUpscale: false
};

const manualRotationDefaults: Required<ImageManualRotationOptions> = {
    stepDegrees: 90
};

const jpegMimeType = mime.getType('jpg')!;
const pngMimeType = mime.getType('png')!;
const webpMimeType = mime.getType('webp')!;
const supportedFormats = new Set([jpegMimeType, pngMimeType, webpMimeType]);

const compressionDefaults: Required<ImageCompressionOptions> = {
    thresholdBytes: 1024 ** 2,
    quality: 0.82,
    fallbackFormat: webpMimeType
};

function resolveFeature<T extends object>(value: boolean | T | undefined, defaults: Required<T>): false | Required<T> {
    if (!value) return false;
    return value === true ? defaults : { ...defaults, ...value } as Required<T>;
}

export function resolveImageProcessingOptions(options?: BrowserImageProcessingOptions): ResolvedBrowserImageProcessingOptions {
    return {
        exifOrientation: options?.exifOrientation ?? BrowserImageProcessingDefaults.exifOrientation,
        crop: resolveFeature(options?.crop, cropDefaults),
        resize: resolveFeature(options?.resize, resizeDefaults),
        manualRotation: resolveFeature(options?.manualRotation, manualRotationDefaults),
        compression: resolveFeature(options?.compression, compressionDefaults),
        outputFormat: options?.outputFormat,
        jpegBackgroundColor: options?.jpegBackgroundColor ?? BrowserImageProcessingDefaults.jpegBackgroundColor
    };
}

function normalizeImageFormat(type: string | null | undefined) {
    if (!type) return undefined;
    const normalizedType = type.toLowerCase();
    if (supportedFormats.has(normalizedType)) return normalizedType;

    const typeFromExtension = mime.getType(normalizedType);
    if (typeFromExtension && supportedFormats.has(typeFromExtension)) return typeFromExtension;

    const extension = mime.getExtension(normalizedType);
    const canonicalType = extension ? mime.getType(extension) : null;
    return canonicalType && supportedFormats.has(canonicalType) ? canonicalType : undefined;
}

export function getSupportedImageMimeType(fileName: string, declaredType?: string) {
    return normalizeImageFormat(declaredType) ?? normalizeImageFormat(mime.getType(fileName));
}

export function isSupportedImageFile(fileName: string, declaredType?: string) {
    return getSupportedImageMimeType(fileName, declaredType) !== undefined;
}

export function getImageOutputSettings(file: File, options: ResolvedBrowserImageProcessingOptions) {
    const sourceFormat = getSupportedImageMimeType(file.name, file.type);
    if (!sourceFormat) {
        throw new Error(`The image format "${file.type || 'unknown'}" cannot be processed in the browser.`);
    }

    const shouldCompress = options.compression !== false && file.size > options.compression.thresholdBytes;
    const requestedOutputFormat = options.outputFormat
        ?? (shouldCompress && sourceFormat === pngMimeType && options.compression !== false
            ? options.compression.fallbackFormat
            : sourceFormat);
    const outputFormat = normalizeImageFormat(requestedOutputFormat);
    if (!outputFormat) {
        throw new Error(`The image format "${requestedOutputFormat || 'unknown'}" cannot be encoded in the browser.`);
    }

    return {
        sourceFormat,
        outputFormat,
        shouldCompress,
        quality: shouldCompress && options.compression !== false && (outputFormat === jpegMimeType || outputFormat === webpMimeType)
            ? options.compression.quality
            : undefined
    };
}

function replaceFileExtension(fileName: string, format: string) {
    const baseName = fileName.replace(/\.[^/.]+$/, '') || 'avatar';
    const extension = mime.getExtension(format);
    if (!extension) throw new Error(`No file extension is registered for ${format}.`);
    return `${baseName}.${extension}`;
}

export async function canvasToProcessedFile(canvas: HTMLCanvasElement, source: File, options: ResolvedBrowserImageProcessingOptions) {
    const { outputFormat, quality } = getImageOutputSettings(source, options);

    if (outputFormat === jpegMimeType) {
        const flattenedCanvas = document.createElement('canvas');
        flattenedCanvas.width = canvas.width;
        flattenedCanvas.height = canvas.height;
        const context = flattenedCanvas.getContext('2d');
        if (!context) throw new Error('The browser could not create an image canvas.');
        context.fillStyle = options.jpegBackgroundColor;
        context.fillRect(0, 0, flattenedCanvas.width, flattenedCanvas.height);
        context.drawImage(canvas, 0, 0);
        canvas = flattenedCanvas;
    }

    const blob = await canvasToBlob(canvas, outputFormat, quality);

    if (blob.type !== outputFormat) {
        throw new Error(`The browser does not support ${outputFormat} image encoding.`);
    }

    return new File([blob], replaceFileExtension(source.name, outputFormat), {
        type: outputFormat,
        lastModified: Date.now()
    });
}

function getExifOrientation(buffer: ArrayBuffer) {
    const view = new DataView(buffer);
    if (view.byteLength < 4 || view.getUint16(0, false) !== 0xffd8) return 1;

    let offset = 2;
    while (offset + 4 <= view.byteLength) {
        const marker = view.getUint16(offset, false);

        offset += 2;

        if ((marker & 0xff00) !== 0xff00) break;

        const length = view.getUint16(offset, false);
        if (length < 2 || offset + length > view.byteLength) break;

        if (marker === 0xffe1 && length >= 10 && view.getUint32(offset + 2, false) === 0x45786966) {
            const tiffOffset = offset + 8;
            const littleEndian = view.getUint16(tiffOffset, false) === 0x4949;
            const firstIfdOffset = view.getUint32(tiffOffset + 4, littleEndian);
            const ifdOffset = tiffOffset + firstIfdOffset;

            if (ifdOffset + 2 > view.byteLength) return 1;

            const entries = view.getUint16(ifdOffset, littleEndian);

            for (let index = 0; index < entries; index++) {
                const entryOffset = ifdOffset + 2 + index * 12;

                if (entryOffset + 12 > view.byteLength) return 1;

                if (view.getUint16(entryOffset, littleEndian) === 0x0112) {
                    return view.getUint16(entryOffset + 8, littleEndian);
                }
            }
        }
        offset += length;
    }
    return 1;
}

export async function processImageFileAutomatically(file: File, options: ResolvedBrowserImageProcessingOptions) {
    if (!getSupportedImageMimeType(file.name, file.type)) {
        const requiresPixelProcessing = options.resize !== false || options.compression !== false || !!options.outputFormat;
        if (!requiresPixelProcessing) return file;
    }

    const settings = getImageOutputSettings(file, options);
    const exifOrientation = settings.sourceFormat === jpegMimeType && options.exifOrientation
        ? getExifOrientation(await file.arrayBuffer())
        : 1;
    const formatChanges = settings.outputFormat !== settings.sourceFormat;

    if (options.resize === false && !settings.shouldCompress && !formatChanges && exifOrientation === 1) {
        return file;
    }

    const image = await loadDecodedImage(file, options.exifOrientation);
    try {
        const { width: originalWidth, height: originalHeight } = getDecodedImageSize(image);
        const resize = options.resize;

        const scale = resize === false
            ? 1
            : Math.min(
                resize.maxWidth / originalWidth,
                resize.maxHeight / originalHeight,
                resize.allowUpscale ? Number.POSITIVE_INFINITY : 1
            );

        if (scale === 1 && !settings.shouldCompress && !formatChanges && exifOrientation === 1) {
            return file;
        }

        const canvas = document.createElement('canvas');

        canvas.width = Math.max(1, Math.round(originalWidth * scale));
        canvas.height = Math.max(1, Math.round(originalHeight * scale));

        const context = canvas.getContext('2d');
        if (!context) throw new Error('The browser could not create an image canvas.');

        context.imageSmoothingEnabled = true;
        context.imageSmoothingQuality = 'high';
        context.drawImage(image, 0, 0, canvas.width, canvas.height);

        return await canvasToProcessedFile(canvas, file, options);
    }
    finally {
        closeDecodedImage(image);
    }
}

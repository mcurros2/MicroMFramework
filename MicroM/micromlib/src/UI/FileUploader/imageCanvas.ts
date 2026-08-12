export type DecodedImage = ImageBitmap | HTMLImageElement;

export function getDecodedImageSize(image: DecodedImage) {
    return image instanceof ImageBitmap
        ? { width: image.width, height: image.height }
        : { width: image.naturalWidth, height: image.naturalHeight };
}

export async function loadDecodedImage(file: File, respectExifOrientation: boolean): Promise<DecodedImage> {
    if ('createImageBitmap' in window) {
        return await createImageBitmap(file, {
            imageOrientation: respectExifOrientation ? 'from-image' : 'none'
        });
    }

    const source = URL.createObjectURL(file);
    try {
        return await new Promise<HTMLImageElement>((resolve, reject) => {
            const element = new Image();
            element.onload = () => resolve(element);
            element.onerror = () => reject(new Error('The browser could not decode the image.'));
            element.src = source;
        });
    }
    finally {
        URL.revokeObjectURL(source);
    }
}

export function closeDecodedImage(image: DecodedImage) {
    if (image instanceof ImageBitmap) image.close();
}

export async function canvasToBlob(canvas: HTMLCanvasElement, type: string, quality?: number) {
    return await new Promise<Blob>((resolve, reject) => {
        canvas.toBlob(
            result => result ? resolve(result) : reject(new Error('The browser could not encode the image.')),
            type,
            quality
        );
    });
}

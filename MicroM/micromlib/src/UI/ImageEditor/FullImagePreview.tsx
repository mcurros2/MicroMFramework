import { useEffect, useRef } from "react";
import { getRotatedSize } from "./imageEditorProcessing";

export interface FullImagePreviewProps {
    source: HTMLCanvasElement,
    rotation: number,
    label: string,
}

export function FullImagePreview({ source, rotation, label }: FullImagePreviewProps) {
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

        if (typeof ResizeObserver === 'undefined') {
            window.addEventListener('resize', draw);
            return () => window.removeEventListener('resize', draw);
        }

        const observer = new ResizeObserver(draw);
        observer.observe(canvas);
        return () => observer.disconnect();
    }, [rotation, source]);

    return <canvas ref={canvasRef} className="image-editor__cropper" role="img" aria-label={label} />;
}

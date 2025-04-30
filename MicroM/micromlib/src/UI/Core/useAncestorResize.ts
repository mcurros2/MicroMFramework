import { useEffect, useRef, useState } from 'react';

type SizeInput = {
    width: string;
    height: string;
};

type Size = {
    width: number;
    height: number;
};

function parseUnit(value: string, ancestorWidth: number, ancestorHeight: number, baseFontSize: number): number {
    if (value.endsWith('px')) return parseFloat(value);
    if (value.endsWith('vw')) return (window.innerWidth * parseFloat(value)) / 100;
    if (value.endsWith('vh')) return (window.innerHeight * parseFloat(value)) / 100;
    if (value.endsWith('rem')) return parseFloat(value) * baseFontSize;
    if (value.endsWith('em')) return parseFloat(value) * baseFontSize;
    if (value.endsWith('%')) {
        return (parseFloat(value) / 100) * (ancestorWidth || ancestorHeight);
    }
    return parseFloat(value);
}

export function useAncestorResize(minSize: SizeInput = { width: '300px', height: '400px' }) {
    const ref = useRef<HTMLDivElement>(null);
    const [size, setSize] = useState<Size>({ width: 0, height: 0 });

    useEffect(() => {
        if (!ref.current) return;


        const findFirstScrollableAncestor = (el: HTMLElement | null): HTMLElement | null => {
            while (el) {
                const overflowY = getComputedStyle(el).overflowY;
                if (overflowY === 'auto' || overflowY === 'scroll') return el;
                el = el.parentElement;
            }
            return null;
        };

        const ancestor = findFirstScrollableAncestor(ref.current.parentElement) ?? ref.current.parentElement;
        if (!ancestor) {
            console.warn('cant resize, noancestor');
            return;
        }

        const baseFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);

        const minWidth = parseUnit(minSize.width, ancestor.clientWidth, ancestor.clientHeight, baseFontSize);
        const minHeight = parseUnit(minSize.height, ancestor.clientWidth, ancestor.clientHeight, baseFontSize);

        const resize = () => {
            const width = ancestor.clientWidth;
            const height = ancestor.clientHeight;

            setSize({
                width: Math.max(minWidth, width),
                height: Math.max(minHeight, height),
            });
        };

        const observer = new ResizeObserver(resize);
        observer.observe(ancestor);

        window.addEventListener('resize', resize);

        resize();

        return () => {
            observer.disconnect();
            window.removeEventListener('resize', resize);
        };
    }, [minSize.width, minSize.height]);

    return { ref, size };
}

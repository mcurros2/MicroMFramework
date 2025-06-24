import { useEffect } from 'react';

/***
 * This hook sets a dynamic CSS variables `--dynVH` and `--dynVW` to the viewport height and width in pixels.
 */
export function useViewportDynamicVariables() {
    useEffect(() => {
        const setDynVariables = () => {
            // window.innerHeight excludes address bar
            const dynVH = window.innerHeight * 0.01;
            document.documentElement.style.setProperty('--dynVH', `${dynVH}px`);
            // set width
            const dynVW = window.innerWidth * 0.01;
            document.documentElement.style.setProperty('--dynVW', `${dynVW}px`);
        };

        setDynVariables();
        window.addEventListener('resize', setDynVariables);
        window.addEventListener('orientationchange', setDynVariables);
        return () => {
            window.removeEventListener('resize', setDynVariables);
            window.removeEventListener('orientationchange', setDynVariables);
        };
    }, []);
}

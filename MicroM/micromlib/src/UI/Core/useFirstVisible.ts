import { useEffect, useRef, useState } from 'react';

// Custom hook to use Intersection Observer
export function useFirstVisible(ref: React.RefObject<HTMLElement>, options?: IntersectionObserverInit) {
    const [isFirstVisible, setIsFirstVisible] = useState<boolean>(false);
    const hasBeenVisible = useRef<boolean>(false);

    useEffect(() => {
        const currentElement = ref.current;
        if (!currentElement) return;

        const observer = new IntersectionObserver(([entry]) => {
            if (entry.isIntersecting && !hasBeenVisible.current) {
                setIsFirstVisible(true);
                hasBeenVisible.current = true;
                observer.disconnect();
            }
        }, options);

        observer.observe(currentElement);

        return () => observer.disconnect();
    }, [ref, options]);

    return isFirstVisible;
};

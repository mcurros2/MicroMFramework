import { useRef, useEffect } from 'react';

export function useEnterAsTab<T extends HTMLElement>() {
    const ref = useRef<T | null>(null);

    useEffect(() => {
        const handleKeyDown = (event: KeyboardEvent) => {
            const activeElement = document.activeElement as HTMLInputElement;
            const validInputTypes = [
                'date', 'datetime-local', 'email', 'month', 'number', 'password', 'search', 'tel', 'text', 'time', 'url', 'week'
            ];

            if (event.key === 'Enter' && activeElement && validInputTypes.includes(activeElement.type)) {
                event.preventDefault();

                const focusableElements: HTMLElement[] = Array.from(
                    ref.current?.querySelectorAll(
                        'a[href], button, textarea, input[type="text"], input[type="radio"], input[type="checkbox"], select'
                    ) || []
                ) as HTMLElement[];
                //console.log(`EnterAsTAB: focusableElements: ${focusableElements}`);

                const currentTabIndex = focusableElements.indexOf(activeElement);
                const nextElement = focusableElements[currentTabIndex + 1] || focusableElements[0];

                //console.log(`EnterAsTAB: nextElement: ${nextElement}`);

                nextElement?.focus();
            }
        };

        const currentRef = ref.current;
        if (currentRef) {
            currentRef.addEventListener('keydown', handleKeyDown);
        }

        // Cleanup function
        return () => {
            if (currentRef) {
                currentRef.removeEventListener('keydown', handleKeyDown);
            }
        };
    }, []);

    return ref;
};


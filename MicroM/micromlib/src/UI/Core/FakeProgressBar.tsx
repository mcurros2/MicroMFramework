import { Progress, ProgressProps } from '@mantine/core';
import { useEffect, useRef, useState } from 'react';

export function FakeProgressBar(props: ProgressProps) {
    const [value, setValue] = useState(0);
    const initialDelay = 10;
    const maxWait = 360000; // 1 hour
        
    const delayRef = useRef(initialDelay);

    useEffect(() => {
        const interval = setInterval(() => {
            setValue((v) => {
                if (v >= 60) {
                    delayRef.current = initialDelay + Math.pow((maxWait / 10) ** (1 / 40), v - 60)
                    clearInterval(interval);
                }
                return Math.min(v + (v >= 60 ? 0.5 : 1), 100);
            });
        }, delayRef.current);

        return () => clearInterval(interval);
    }, [value]);

    return <Progress value={value} {...props} />;
};

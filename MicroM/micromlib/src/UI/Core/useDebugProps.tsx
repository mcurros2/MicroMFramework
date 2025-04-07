import { useEffect, useMemo, useRef } from "react";


export function useDebugProps<T>(propsObject: T, dependencies: any[], componentName: string) {
    const prevProps = useRef<T>(propsObject);

    const memoizedProps = useMemo(() => propsObject, dependencies);

    useEffect(() => {
        const changedProps: Record<string, { prev: any, actual: any }>[] = [];
        const props: T = propsObject;

        for (const prop in props) {
            if (prevProps.current[prop as keyof T] !== props[prop as keyof T]) {
                const changedValue: Record<string, { prev: any, actual: any }> = {
                    [prop]: { prev: prevProps.current[prop as keyof T], actual: props[prop as keyof T] }
                };
                changedProps.push(changedValue);
            }

        }

        console.log(`${componentName} Changed props:`, changedProps);

        prevProps.current = props;
    }, [memoizedProps, componentName]);
}
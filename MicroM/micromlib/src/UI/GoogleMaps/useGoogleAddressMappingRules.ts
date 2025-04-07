import { useCallback, useRef } from "react";
import { AddressMappingRule } from "./Mapping.types";

export function useGoogleAddressMappingRules() {
    const mappingRules = useRef<AddressMappingRule[]>([]);

    const addMappingRule = useCallback((rules: AddressMappingRule[]): void => {
        for (const newRule of rules) {
            mappingRules.current.push(newRule);
        }
        // Sorting could potentially be optimized if performance becomes an issue
        mappingRules.current.sort((a, b) => Object.keys(b.conditions).length - Object.keys(a.conditions).length);
    }, []);


    const clearMappingRules = useCallback(() => {
        mappingRules.current = [];
    }, []);

    return {
        mappingRules: mappingRules.current,
        addMappingRule,
        clearMappingRules
    }

}
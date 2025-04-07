import { useCallback } from "react";
import { DefaultAddressMapping } from "./mappingRulesDefinition";
import { AddressComponentResult, AddressMappingRule, DefaultAddressMappingType, GoogleMapsAddressComponentType, GoogleMapsMappingRules, MappedAddressResult } from "./Mapping.types";

function convertAddressComponentsToAddressComponentResult(addressComponents: google.maps.GeocoderAddressComponent[]): AddressComponentResult {
    const result: AddressComponentResult = {};

    for (let i = 0; i < addressComponents.length; i++) {
        const component = addressComponents[i];

        // MMC: we only take the first type of the component as is the last level of the hierearchy
        const type = component.types[0] as GoogleMapsAddressComponentType;

        if (!result[type]) {
            result[type] = {
                long_name: component.long_name,
                short_name: component.short_name
            };
        }
    }

    return result;
}

function doesRuleMatch(conditions: AddressComponentResult, addressComponentResult: AddressComponentResult): boolean {
    return Object.entries(conditions).every(([key, value]) => {
        const tkey = key as GoogleMapsAddressComponentType;
        return addressComponentResult[tkey]?.long_name === value.long_name || addressComponentResult[tkey]?.short_name === value.short_name
    });

}

function applyRuleMappings(
    target: MappedAddressResult,
    source: AddressComponentResult,
    ruleMappings: GoogleMapsMappingRules
) {
    for (const [mappedKey, mappings] of Object.entries(ruleMappings)) {
        for (const mapping of mappings) {
            for (const [sourceKey, valueKey] of Object.entries(mapping)) {
                const skey = sourceKey as GoogleMapsAddressComponentType;
                if (source[skey]) {
                    target[mappedKey as keyof DefaultAddressMappingType] = source[skey]![valueKey];
                    break; // Apply the first mapping found for this key
                }
            }
        }
    }
}

function applyDefaultMappings(
    target: MappedAddressResult,
    source: AddressComponentResult,
    defaultMappings: DefaultAddressMappingType
) {
    for (const [targetKey, mappings] of Object.entries(defaultMappings)) {
        const tkey = targetKey as keyof MappedAddressResult;
        // If the key has already been set by a specific rule, skip applying the default mapping
        if (target[tkey] !== undefined) continue;

        for (const mapping of mappings) {
            for (const [sourceKey, valueKey] of Object.entries(mapping)) {
                const skey = sourceKey as GoogleMapsAddressComponentType;
                if (source[skey]) {
                    target[targetKey as keyof DefaultAddressMappingType] = source[skey]![valueKey];
                    break; // Apply the first default mapping found for this key
                }
            }
        }
    }
}

function mapAddressComponentResultToMappedAddress(
    addressComponentResult: AddressComponentResult,
    rules: AddressMappingRule[]
): MappedAddressResult {

    const mappedAddress: MappedAddressResult = {};

    // Determine the mapped address type
    if (addressComponentResult.street_number || addressComponentResult.route) {
        mappedAddress.mappedAddressType = 'address';
    }
    else if (addressComponentResult.postal_code) {
        mappedAddress.mappedAddressType = 'postal_code';
    }
    else if (addressComponentResult.administrative_area_level_2) {
        mappedAddress.mappedAddressType = 'county';
    }
    else if (addressComponentResult.locality) {
        mappedAddress.mappedAddressType = 'city';
    }
    else if (addressComponentResult.administrative_area_level_1) {
        mappedAddress.mappedAddressType = 'state_province';
    }
    else if (addressComponentResult.country) {
        mappedAddress.mappedAddressType = 'country';
    }
    else {
        mappedAddress.mappedAddressType = 'unknown';
    }

    if (!rules) return mappedAddress;

    // Check each rule to find the first that matches the conditions
    let apply_default_mappings = true;
    for (const rule of rules) {
        if (doesRuleMatch(rule.conditions, addressComponentResult)) {
            if (rule.dontApplyDefaultMappings) apply_default_mappings = false;
            // Apply specific rule mappings
            applyRuleMappings(mappedAddress, addressComponentResult, rule.mapping);
            // Break after finding and applying the first matching rule
            break;
        }
    }

    // Apply default mappings for any keys not set by the specific rule
    if (apply_default_mappings) applyDefaultMappings(mappedAddress, addressComponentResult, DefaultAddressMapping);


    return mappedAddress;
}


export function useGoogleAddressMapping() {

    const mapGoogleAddressComponents = useCallback((addressComponents: google.maps.GeocoderAddressComponent[], mappingRules: AddressMappingRule[]): MappedAddressResult => {
        const addressComponentResult = convertAddressComponentsToAddressComponentResult(addressComponents);
        return mapAddressComponentResultToMappedAddress(addressComponentResult, mappingRules);
    }, []);

    return {
        mapGoogleAddressComponents
    }

}
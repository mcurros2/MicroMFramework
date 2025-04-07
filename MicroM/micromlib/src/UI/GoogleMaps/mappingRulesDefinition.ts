import { AddressMappingRule, DefaultAddressMappingType } from "./Mapping.types";

export const DefaultAddressMapping: DefaultAddressMappingType = {
    country: [{ country: 'long_name' }],
    countryCode: [{ country: 'short_name' }],
    province: [{ administrative_area_level_1: 'long_name' }],
    provinceCode: [{ administrative_area_level_1: 'short_name' }],
    department: [{ locality: 'long_name' }],
    departmentCode: [{ locality: 'short_name' }],
    city: [{ administrative_area_level_2: 'long_name' }],
    cityCode: [{ administrative_area_level_2: 'short_name' }],
    street: [{ route: 'long_name' }],
    streetNumber: [{ street_number: 'long_name' }],
    apartment: [{ room: 'long_name' }],
    floor: [{ floor: 'long_name' }],
    postalCode: [{ postal_code: 'long_name' }],
}


export const ARMappingRules: AddressMappingRule = {
    conditions: { country: { short_name: 'AR' }, locality: { short_name: 'CABA' } },
    mapping: {
        city: [{ administrative_area_level_1: 'long_name' }],
        cityCode: [{ administrative_area_level_1: 'short_name' }],
        department: [{ sublocality_level_1: 'long_name' }],
        departmentCode: [{ sublocality_level_1: 'short_name' }],
    }
}

export const UYMappingRules: AddressMappingRule = {
    conditions: { country: { short_name: 'UY' } },
    mapping: {
        city: [{ locality: 'long_name' }],
        cityCode: [{ locality: 'short_name' }],
        department: [{ neighborhood: 'long_name', locality: 'long_name' }],
        departmentCode: [{ neighborhood: 'short_name', locality: 'short_name' }],
    }
}

export const USMappingRules: AddressMappingRule = {
    conditions: { country: { short_name: 'US' } },
    dontApplyDefaultMappings: true,
    mapping: {
        country: [{ country: 'long_name' }],
        countryCode: [{ country: 'short_name' }],
        province: [{ administrative_area_level_1: 'long_name' }],
        provinceCode: [{ administrative_area_level_1: 'short_name' }],

        // county
        department: [{ administrative_area_level_2: 'long_name' }],
        departmentCode: [{ administrative_area_level_2: 'short_name' }],

        city: [{ locality: 'long_name' }],
        cityCode: [{ locality: 'short_name' }],

        street: [{ route: 'long_name' }],
        streetNumber: [{ street_number: 'long_name' }],

        apartment: [{ subpremise: 'long_name' }],

        floor: [{ floor: 'long_name' }],
        postalCode: [{ postal_code: 'long_name' }],

    }
}


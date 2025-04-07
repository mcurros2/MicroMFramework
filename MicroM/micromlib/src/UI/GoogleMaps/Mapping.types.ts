import { latLng } from "../Core/types";

export const DEFAULT_MAP_CENTER: latLng = { lat: -34.603683, lng: -58.381557 };


export type GoogleMapsAddressComponentType =
    | 'street_address' | 'route' | 'intersection' | 'political' | 'country' | 'administrative_area_level_1' | 'administrative_area_level_2'
    | 'administrative_area_level_3' | 'administrative_area_level_4' | 'administrative_area_level_5' | 'administrative_area_level_6' | 'administrative_area_level_7'
    | 'colloquial_area' | 'locality' | 'sublocality' | 'sublocality_level_1' | 'sublocality_level_2' | 'sublocality_level_3'
    | 'sublocality_level_4' | 'sublocality_level_5' | 'neighborhood' | 'premise' | 'subpremise' | 'plus_code' | 'postal_code'
    | 'natural_feature' | 'airport' | 'park' | 'point_of_interest' | 'floor' | 'establishment' | 'landmark' | 'parking' | 'post_box'
    | 'postal_town' | 'room' | 'street_number' | 'bus_station' | 'train_station' | 'transit_station';

export type AddressComponentValue = { long_name?: string, short_name?: string }

// MMC: this is the result of the google maps api converted to be processed by the address mapping rules
export type AddressComponentResult = Partial<Record<GoogleMapsAddressComponentType, AddressComponentValue>>;

export type MappingAddressType = 'address' | 'postal_code' | 'state_province' | 'city' | 'country' | 'county' | 'unknown';

// MMC: this is the result of the address mapping rules applied to the google maps api result
export interface MappedAddressResult {
    street?: string,
    streetNumber?: string,
    apartment?: string,
    floor?: string,
    country?: string,
    countryCode?: string,
    province?: string,
    provinceCode?: string,
    city?: string,
    cityCode?: string,

    // county
    department?: string,
    departmentCode?: string,

    postalCode?: string,
    mappedAddressType?: MappingAddressType;
}

export const AddressSearchDefaultZoomLevels: Record<MappingAddressType, number> = {
    address: 17,
    postal_code: 14,
    city: 12,
    state_province: 6,
    country: 4,
    county: 12,
    unknown: 12
}


export type GoogleAddressMappingRecord = Partial<Record<GoogleMapsAddressComponentType, keyof AddressComponentValue>>;

export type GoogleMapsMappingRules = Partial<Record<keyof Omit<MappedAddressResult, 'mappedAddressType'>, GoogleAddressMappingRecord[]>>;

export interface AddressMappingRule {
    conditions: AddressComponentResult,
    mapping: GoogleMapsMappingRules,
    dontApplyDefaultMappings?: boolean
}

export type DefaultAddressMappingType = Record<keyof Omit<MappedAddressResult, 'mappedAddressType'>, GoogleAddressMappingRecord[]>;

export interface GoogleMapsErrorStatus {
    origin?: string,
    status?: string,
}

export interface PlacesOperationResult {
    status?: string,
    result?: google.maps.places.PlaceResult,
    mapped?: MappedAddressResult
}
export interface GeocodeOperationResult {
    status?: string,
    result?: google.maps.GeocoderResult
}

export type MapOptions = google.maps.MapOptions

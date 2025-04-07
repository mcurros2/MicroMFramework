import { RefObject, useCallback, useMemo, useRef } from "react";
import { ValuesRecord } from "../../client";


export type GroupRecord = {
    recordIndex: number,
    recordValue: ValuesRecord
}

export interface LocationGroup {
    locationKey: string,
    location: google.maps.LatLng | google.maps.LatLngLiteral,
    records: GroupRecord[]
}

export interface RecordGroupManager {
    locationGroups: RefObject<LocationGroup[]>,
    getGroupByLocation: (location: google.maps.LatLng | google.maps.LatLngLiteral) => LocationGroup | undefined,
    addRecordToGroup: (location: google.maps.LatLng | google.maps.LatLngLiteral, recordIndex: number, recordValue: ValuesRecord) => LocationGroup,
    clearAllGroups: () => void,
    clearOneRecordGroups: () => void,
}

export function useRecordGroupManager(): RecordGroupManager {
    const locationGroups = useRef<LocationGroup[]>([]);
    const locationMap = useRef<Map<string, LocationGroup>>(new Map());

    const addRecordToGroup = useCallback((location: google.maps.LatLng | google.maps.LatLngLiteral, recordIndex: number, recordValue: ValuesRecord) => {
        const locationKey = `${location.lat}_${location.lng}`;
        let group = locationMap.current.get(locationKey);
        if (!group) {
            group = { locationKey, location, records: [] };
            locationGroups.current.push(group);
            locationMap.current.set(locationKey, group);
        }
        group.records.push({ recordIndex, recordValue });
        return group;
    }, []);

    const getGroupByLocation = useCallback((location: google.maps.LatLng | google.maps.LatLngLiteral) => {
        const locationKey = `${location.lat}_${location.lng}`;
        return locationMap.current.get(locationKey);
    }, []);

    const clearAllGroups = useCallback(() => {
        locationGroups.current = [];
        locationMap.current.clear();
    }, []);

    const clearOneRecordGroups = useCallback(() => {
        locationGroups.current.forEach(group => {
            if (group.records.length === 1) {
                locationMap.current.delete(group.locationKey);
            }
        });
        locationGroups.current = locationGroups.current.filter(group => group.records.length > 1);
    }, []);

    const manager = useMemo<RecordGroupManager>(() => ({
        locationGroups,
        getGroupByLocation,
        addRecordToGroup,
        clearAllGroups,
        clearOneRecordGroups,
    }), [addRecordToGroup, clearAllGroups, clearOneRecordGroups, getGroupByLocation]);

    return manager;

}
import { Skeleton } from "@mantine/core";
import { useEffect } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { DataResult, ValuesObject } from "../../client";
import { AlertError, useExecuteProc } from "../Core";
import { ClickedRegionData, SelectedRegionData } from "../GoogleMaps";
import { RegionSelector, RegionSelectorProps } from "../RegionSelector";


export interface DataRegionSelectorProps extends RegionSelectorProps {
    entity: Entity<EntityDefinition>,
    queryName: string,
    parentKeys?: ValuesObject,
    mapDataResult: (data: DataResult) => Record<string, SelectedRegionData>,
}

export function DataRegion(props: DataRegionSelectorProps) {
    const {
        entity, queryName, parentKeys, mapId, countries, mapCenter, mapHeight, mapDataResult,
        selectedRegions, setSelectedRegions, clickedRegions, setClickedRegions, mapRestrictions,
        maxRegions, onMaxRegionsReached
    } = props;

    const query = useExecuteProc(entity, entity.def.procs[queryName]);

    useEffect(() => {
        const refresh = async () => {
            const proc = entity.def.procs[queryName];
            if (!proc) {
                console.error(`Proc ${queryName} not found in entity ${entity.def.name}`);
                return;
            }

            const result = await query.execute(parentKeys || {});
            if (result?.data) {
                const regions = mapDataResult(result.data[0]);
                setSelectedRegions(regions);

                const clickedRegionsData: Record<string, ClickedRegionData> = Object.entries(regions).reduce((acc, [key, value]) => {
                    acc[key] = {
                        featuretype: value.featuretype,
                        placeId: value.placeId
                    };
                    return acc;
                }, {} as Record<string, ClickedRegionData>);

                setClickedRegions(clickedRegionsData);
            }
        }
        refresh();
    }, [entity.def.name, entity.def.procs, mapDataResult, parentKeys, query, queryName, setClickedRegions, setSelectedRegions]);

    return (
        <>
            {query.status.loading && <Skeleton height={mapHeight} />}
            {query.status.error &&
                <AlertError mb="xs">{query.status.error.status} {query.status.error.message} {query.status.error.statusMessage}</AlertError>
            }
            {!query.status.loading && !query.status.error &&
                <RegionSelector
                    mapId={mapId}
                    mapHeight={mapHeight}
                    countries={countries}
                    mapCenter={mapCenter}
                    selectedRegions={selectedRegions}
                    setSelectedRegions={setSelectedRegions}
                    clickedRegions={clickedRegions}
                    setClickedRegions={setClickedRegions}
                    mapRestrictions={mapRestrictions}
                    maxRegions={maxRegions}
                    onMaxRegionsReached={onMaxRegionsReached}
                />
            }
        </>
    )
}
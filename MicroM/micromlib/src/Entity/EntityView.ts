import { ComponentType } from "react";
import { DataMapProps, EntityCardProps, GridColumnsOverrides } from "../UI";
import { ValuesObject } from "../client";
import { EntityConstructor } from "./EntityLookup";
import { MantineTheme } from "@mantine/core";

export interface EntityView {
    name: string;
    keyMappings: Record<string, number>;
    Card?: Promise<ComponentType<EntityCardProps<ValuesObject>>> | null;
    FiltersEntity?: EntityConstructor,
    gridColumnsOverrides?: (theme: MantineTheme) => GridColumnsOverrides;
    mapMarkerRenderer?: DataMapProps['markerRenderer'];
    mapGroupMarkerRenderer?: DataMapProps['groupMarkerRenderer'];
    mapInfoWindowRenderer?: DataMapProps['InfoWindowRenderer'];
    mapSelectRecordsRenderer?: DataMapProps['selectRecordsRenderer'];
}

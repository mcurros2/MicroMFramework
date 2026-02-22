import { MantineTheme } from "@mantine/core";
import { ComponentType } from "react";
import { ValuesObject } from "../client";
import { DataMapProps, EntityCardProps, GridColumnsOverrides } from "../UI";
import { EntityConstructor } from "./EntityLookup";

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

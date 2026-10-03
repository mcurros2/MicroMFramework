import { Alert, Anchor, Button, Group, Stack, Text, TextInput } from "@mantine/core";
import { GoogleMapsAPILoaderConfig } from "@mcurros2/microm";
import { IconMapPinOff } from "@tabler/icons-react";
import { PropsWithChildren, useState } from "react";

const KEY_STORAGE = "microm-storybook:google-maps-api-key";
const MAP_ID_STORAGE = "microm-storybook:google-maps-map-id";

function stored(key: string) {
    try { return window.localStorage.getItem(key) ?? ""; } catch { return ""; }
}

export const googleMapsApiKey: string = import.meta.env.STORYBOOK_GOOGLE_MAPS_API_KEY || stored(KEY_STORAGE);
export const googleMapsMapId: string = import.meta.env.STORYBOOK_GOOGLE_MAPS_MAP_ID || stored(MAP_ID_STORAGE) || "DEMO_MAP_ID";

// The loader is a singleton created on first use, so the key must be set before any map component mounts.
GoogleMapsAPILoaderConfig.apiKey = googleMapsApiKey;

export function GoogleMapsKeyForm() {
    const [key, setKey] = useState(stored(KEY_STORAGE));
    const [mapId, setMapId] = useState(stored(MAP_ID_STORAGE));
    const save = () => {
        try {
            window.localStorage.setItem(KEY_STORAGE, key.trim());
            window.localStorage.setItem(MAP_ID_STORAGE, mapId.trim());
        } catch {
            // storage unavailable
        }
        window.location.reload();
    };
    return (
        <Stack maw="36rem">
            <Text size="sm">
                Map stories need a Google Maps JavaScript API key (Maps, Places and Geocoding). Set <code>STORYBOOK_GOOGLE_MAPS_API_KEY</code> and
                optionally <code>STORYBOOK_GOOGLE_MAPS_MAP_ID</code> in <code>StoryBook/.env.local</code>, or store them in this browser below.
                RegionSelector and DataRegion also need a vector Map ID with data-driven styling.
            </Text>
            <TextInput label="API key" value={key} onChange={e => setKey(e.currentTarget.value)} type="password" />
            <TextInput label="Map ID" value={mapId} onChange={e => setMapId(e.currentTarget.value)} placeholder="DEMO_MAP_ID" />
            <Group><Button onClick={save}>Save and reload</Button></Group>
            <Text size="xs" color="dimmed">
                Current source: {import.meta.env.STORYBOOK_GOOGLE_MAPS_API_KEY ? ".env" : stored(KEY_STORAGE) ? "localStorage" : "not configured"}.{" "}
                <Anchor href="https://developers.google.com/maps/documentation/javascript/get-api-key" target="_blank">How to get a key</Anchor>
            </Text>
        </Stack>
    );
}

export function RequiresGoogleMaps({ children }: PropsWithChildren) {
    if (googleMapsApiKey) return <>{children}</>;
    return (
        <Alert icon={<IconMapPinOff size="1rem" />} title="Google Maps API key not configured" color="yellow">
            <Text size="sm" mb="sm">This component loads Google Maps. Configure a key in the story "MicroM/Maps/Setup" or in <code>.env.local</code>.</Text>
            <GoogleMapsKeyForm />
        </Alert>
    );
}

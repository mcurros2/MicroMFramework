import { Group, TextInput } from "@mantine/core";
import { useEffect, useState } from "react";
import { GoogleMapsAPILoaderConfig } from "../../src";


export function GoogleMapsAPIKey() {
    const [apiKey, setApiKey] = useState<string>("");

    useEffect(() => {
        GoogleMapsAPILoaderConfig.apiKey = apiKey;
    }, [apiKey]);

    return (
        <Group key="gmapskey">
            <TextInput label="Google MAPS API Key"
                value={apiKey}
                onChange={(e) => setApiKey(e.currentTarget.value)}
                placeholder="Enter your google maps API key"
            />
        </Group>
    );
}
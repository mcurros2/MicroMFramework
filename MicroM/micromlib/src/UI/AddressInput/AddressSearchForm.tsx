import { Button, Card, Group, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCircleCheck, IconCircleX, IconInfoCircle } from "@tabler/icons-react";
import { useCallback, useState } from "react";
import { latLng } from "../Core";
import { AddressFoundResult } from "../GoogleMaps";
import { AddressMappingRule } from "../GoogleMaps/Mapping.types";
import { AddressSearch } from "./AddressSearch";
import { AddressMapMarkerPosition } from "./AddressMapMarker";



export interface AddressSearchFormProps {
    helpMessage?: string,
    okLabel?: string,
    cancelLabel?: string,

    countries: string[],
    initialSearchAddress?: string,
    currentPosition?: AddressMapMarkerPosition,

    onCancel?: () => void,
    onOK: (addressResult?: AddressFoundResult, position?: latLng) => void

    mappingRules?: AddressMappingRule[],
    draggable?: boolean,
}

export const AddressSearchFormDefaultProps: Partial<AddressSearchFormProps> = {
    helpMessage: "Enter the address to search for. You can move the Marker to another position. Click OK when ready",
    okLabel: "OK",
    cancelLabel: "Cancel",
    mappingRules: []
}


export function AddressSearchForm(props: AddressSearchFormProps) {
    const theme = useMantineTheme();

    const {
        helpMessage, okLabel, onOK, onCancel, cancelLabel, countries, initialSearchAddress, currentPosition, mappingRules, draggable
    } = useComponentDefaultProps('AddressSearchForm', AddressSearchFormDefaultProps, props);

    const [address, setAddress] = useState<AddressFoundResult>();
    const [position, setPosition] = useState<latLng>();

    const handleOnAddressFound = useCallback((result: AddressFoundResult) => {
        setAddress(result);
        if (result.place.geometry && result.place.geometry.location) setPosition({ lat: result.place.geometry.location.lat(), lng: result.place.geometry.location.lng() });
    }, []);

    const handleOnMarkerChanged = useCallback((position: latLng) => {
        if (position) setPosition(position);
    }, []);

    const handleOK = useCallback(() => {
        onOK(address, position);
    }, [address, onOK, position]);

    return (
        <>
            <Card shadow="sm" withBorder={theme.colorScheme === 'dark' ? false : true}>
                <Card.Section p="xs" bg={theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors[theme.primaryColor][3]} mb="1rem">
                    <Group sx={{ gap: "0.25rem" }}>
                        <IconInfoCircle size="1.1rem" />
                        <Text fz="xs" c="dimmed">{helpMessage}</Text>
                    </Group>
                </Card.Section>
                <AddressSearch
                    countries={countries}
                    initialSearchAddress={initialSearchAddress}
                    onAddressFound={handleOnAddressFound}
                    onMarkerChanged={handleOnMarkerChanged}
                    markerPosition={currentPosition}
                    mappingRules={mappingRules}
                    draggable={draggable}
                />
            </Card><Group mt="md" position="right">
                <Button variant="light" leftIcon={<IconCircleX size="1.5rem" />} onClick={() => (onCancel) ? onCancel() : null}>{cancelLabel}</Button>
                <Button onClick={handleOK} color={theme.colors.green[5]} leftIcon={<IconCircleCheck size="1.5rem" />}>{okLabel}</Button>
            </Group>
        </>
    );

}
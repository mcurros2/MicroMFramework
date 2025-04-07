import { Button, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconMapPin } from "@tabler/icons-react";
import { useCallback } from "react";
import { ActionIconVariant, latLng } from "../Core";
import { AddressFoundResult } from "../GoogleMaps";
import { AddressAutocomplete, AddressAutocompleteProps, AddressAutocompleteResult } from "./AddressAutocomplete";
import { AddressMapMarkerPosition } from "./AddressMapMarker";
import { useAddressSearchForm } from "./useAddressSearchForm";

export interface AddressLookupProps extends Omit<AddressAutocompleteProps, 'rightSection'> {
    icon?: React.ReactNode,
    iconLabel?: string,
    iconVariant?: ActionIconVariant,
    currentPosition?: AddressMapMarkerPosition,

}

export const AddressLookupDefaultProps: Partial<AddressLookupProps> = {
    icon: <IconMapPin size="1rem" stroke="1.5" />,
    iconVariant: "light",
    iconLabel: "Map",
    rightSectionWidth: "5.2rem",
}

export function AddressLookup(props: AddressLookupProps) {
    const {
        countries, icon, iconVariant, column, entityForm, currentPosition, onAddressFound,
        mappingRules, iconLabel, rightSectionWidth,
        ...rest
    } = useComponentDefaultProps('AddressLookup', AddressLookupDefaultProps, props);

    const theme = useMantineTheme();
    const search = useAddressSearchForm();

    const handleOKSearch = useCallback((result?: AddressFoundResult, position?: latLng) => {
        const description = result?.suggestionDescription || result?.place.formatted_address;
        if (description) entityForm.form.values[column.name] = description;
        const found: AddressAutocompleteResult = {
            address: result?.address,
            position: position,
            utcOffsetMinutes: result?.place.utc_offset_minutes,
            formattedAddress: description
        };
        if (onAddressFound) onAddressFound(found);
    }, [column.name, entityForm.form, onAddressFound]);

    const handleSearch = useCallback(() => {
        search({
            countries: countries,
            search: entityForm.form.values[column.name] as string,
            currentPosition: currentPosition,
            mappingRules: mappingRules,
            onOK: handleOKSearch
        });
    }, [column.name, countries, currentPosition, entityForm.form.values, handleOKSearch, mappingRules, search]);

    return (
        <>
            <AddressAutocomplete
                {...rest}
                column={column}
                entityForm={entityForm}
                countries={countries}

                onAddressFound={onAddressFound}
                rightSection={<Button color={theme.primaryColor} onClick={handleSearch} variant={iconVariant} size="xs" leftIcon={icon}>{iconLabel}</Button>}
                rightSectionWidth={rightSectionWidth}
            />
        </>
    )
}
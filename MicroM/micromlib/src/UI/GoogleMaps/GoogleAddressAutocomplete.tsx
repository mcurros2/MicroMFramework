import { Autocomplete, AutocompleteProps, Box, ComboboxItem, Group, MantineColor, Text, useProps } from "@mantine/core";
import { ReactNode } from "react";
import { latLng } from "../Core";
import { AddressMappingRule, MappedAddressResult } from "./Mapping.types";
import { useGoogleAddressAutocomplete } from "./useGoogleAddressAutocomplete";


export interface AddressFoundResult {
    place: google.maps.places.PlaceResult,
    address?: MappedAddressResult,
    suggestionDescription?: string,
    position?: latLng
}

export type AddressAutocompleteItem = ComboboxItem & {
    structuredFormatting: google.maps.places.StructuredFormatting,
    place_id: string,
    icon: ReactNode
}

export type GoogleAddressAutocompleteRestrictions = google.maps.places.ComponentRestrictions
export type OnAddressFoundCallback = (result: AddressFoundResult) => void
export type OnInputChangeCallback = (value: string) => void

export type GoogleAddressAutocompleteProps = Omit<AutocompleteProps, 'data'> & {
    restrictions?: GoogleAddressAutocompleteRestrictions,
    onAddressFound?: OnAddressFoundCallback,
    onAPIError?: (status: google.maps.places.PlacesServiceStatus) => void,
    mappingRules?: AddressMappingRule[],
    iconColor?: MantineColor,
}

export const DefaultGoogleAddressAutocompleteProps: Partial<GoogleAddressAutocompleteProps> = {
    label: "Address:",
    placeholder: "Start typing an address to see suggestions",
    mappingRules: [],
}

export function GoogleAddressAutocomplete(props: GoogleAddressAutocompleteProps) {
    props = useProps('GoogleAddressAutocomplete', DefaultGoogleAddressAutocompleteProps, props);

    const {
        onAddressFound, restrictions, onChange, value, onAPIError, mappingRules, iconColor,
        ...rest } = props;

    const googleAddressAutocompleteAPI = useGoogleAddressAutocomplete({ onAddressFound, restrictions, onChange, value, onAPIError, mappingRules, iconColor });


    return <>
        <Box pos={"relative"}>
            <Autocomplete
                {...rest}
                ref={googleAddressAutocompleteAPI.userInputRef}
                onChange={googleAddressAutocompleteAPI.handleOnUserInputChange}
                onOptionSubmit={googleAddressAutocompleteAPI.handleOnOptionSubmit}
                renderOption={({ option }) => {
                    const addressOption = option as unknown as AddressAutocompleteItem;
                    return (
                        <Group wrap="nowrap">
                            {addressOption.icon}
                            <div>
                                <Text>{addressOption.structuredFormatting.main_text}</Text>
                                <Text size="xs" color="dimmed">
                                    {addressOption.structuredFormatting.secondary_text}
                                </Text>
                            </div>
                        </Group>
                    );
                }}
                data={googleAddressAutocompleteAPI.suggestions}
                disabled={googleAddressAutocompleteAPI.isLoading}
                value={googleAddressAutocompleteAPI.value}
                // MMC: show all google prediction results
                filter={({ options }) => options}
            />
            <div ref={googleAddressAutocompleteAPI.attributionsRef}></div>
        </Box>
    </>
}

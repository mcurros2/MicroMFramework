import { Autocomplete, AutocompleteProps, Box, Group, MantineColor, SelectItemProps, Text, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, forwardRef } from "react";
import { AddressMappingRule, MappedAddressResult } from "./Mapping.types";
import { useGoogleAddressAutocomplete } from "./useGoogleAddressAutocomplete";
import { latLng } from "../Core";


export interface AddressFoundResult {
    place: google.maps.places.PlaceResult,
    address?: MappedAddressResult,
    suggestionDescription?: string,
    position?: latLng
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
    props = useComponentDefaultProps('GoogleAddressAutocomplete', DefaultGoogleAddressAutocompleteProps, props);

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
                onItemSubmit={googleAddressAutocompleteAPI.handleOnItemSubmit}
                itemComponent={AddressSuggestionItem}
                data={googleAddressAutocompleteAPI.suggestions}
                disabled={googleAddressAutocompleteAPI.isLoading}
                value={googleAddressAutocompleteAPI.value}
                // MMC: show all google prediction results
                filter={() => true}
            />
            <div ref={googleAddressAutocompleteAPI.attributionsRef}></div>
        </Box>
    </>
}

interface AddressSuggestionItemProps extends SelectItemProps {
    structuredFormatting: google.maps.places.StructuredFormatting,
    place_id: string,
    icon: ReactNode
}

const AddressSuggestionItem = forwardRef<HTMLDivElement, AddressSuggestionItemProps>(

    function AddressSuggestionItem({ structuredFormatting, icon, ...rest }: AddressSuggestionItemProps, ref) {
        return <div ref={ref} {...rest}>
            <Group noWrap>
                {icon}
                <div>
                    <Text>{structuredFormatting.main_text}</Text>
                    <Text size="xs" color="dimmed">
                        {structuredFormatting.secondary_text}
                    </Text>
                </div>
            </Group>
        </div>
    }
);
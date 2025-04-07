import { AutocompleteItem, useMantineTheme } from "@mantine/core";
import { useDebouncedState } from "@mantine/hooks";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useGoogleMapsAPI } from "../../GoogleMapsAPI/useGoogleMapsAPI";
import { GoogleAddressAutocompleteProps } from "./GoogleAddressAutocomplete";
import { MappedAddressResult } from "./Mapping.types";
import { useGoogleAddressMapping } from "./useGoogleAddressMapping";

export function useGoogleAddressAutocomplete({ onAddressFound, restrictions, onChange, value, onAPIError, mappingRules, iconColor }: GoogleAddressAutocompleteProps) {

    const userInputRef = useRef<HTMLInputElement>(null);

    const lastPlaceIdRef = useRef<string>('');

    const [isLoading, setIsLoading] = useState(false);
    const [suggestions, setSuggestions] = useState<(AutocompleteItem & { structuredFormatting: google.maps.places.StructuredFormatting })[]>([]);
    const [query, setQuery] = useDebouncedState('', 700, { leading: true });
    const [inputValue, setInputValue] = useState<string>();
    const [apiError, setApiError] = useState<google.maps.places.PlacesServiceStatus | undefined>();

    const { refreshSessionToken, getAutocompletePredictions, getAutocompleteDetails, attributionsRef } = useGoogleMapsAPI();

    const { mapGoogleAddressComponents } = useGoogleAddressMapping();

    const theme = useMantineTheme();

    const icon = useMemo(() =>
        <svg  xmlns="http://www.w3.org/2000/svg"  width="24"  height="24"  viewBox="0 0 24 24"  fill={theme.colors[iconColor || theme.primaryColor][theme.fn.primaryShade(theme.colorScheme)]}  className="icon icon-tabler icons-tabler-filled icon-tabler-map-pin"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M18.364 4.636a9 9 0 0 1 .203 12.519l-.203 .21l-4.243 4.242a3 3 0 0 1 -4.097 .135l-.144 -.135l-4.244 -4.243a9 9 0 0 1 12.728 -12.728zm-6.364 3.364a3 3 0 1 0 0 6a3 3 0 0 0 0 -6z" /></svg>
        , [iconColor, theme.colorScheme, theme.colors, theme.fn, theme.primaryColor]);

    //workaround for firing an event from the effect
    const onChangeRef = useRef(onChange);
    onChangeRef.current = onChange;
    function handleOnUserInputChange(value: string) {
        setQuery(value);
        setInputValue(value);
    }

    const mapPlaceToAddress = useCallback((found: google.maps.places.PlaceResult): MappedAddressResult => {
        return found.address_components ? mapGoogleAddressComponents(found.address_components, mappingRules!) : {};
    }, [mapGoogleAddressComponents, mappingRules]);

    const handleItemSelected = useCallback((item: AutocompleteItem & { place_id: string }) => {
        if (lastPlaceIdRef.current === item.place_id) return;
        lastPlaceIdRef.current = "";

        setIsLoading(true);

        getAutocompleteDetails({
            placeId: item.place_id, fields: ["address_components", "formatted_address", "geometry.location", "utc_offset_minutes"], callback: (place, status) => {
                lastPlaceIdRef.current = item.place_id;

                setIsLoading(false);
                setSuggestions([]); //to not show the list every time it receives focus

                if (status === google.maps.places.PlacesServiceStatus.OK && place) {
                    if (onAddressFound) {
                        onAddressFound({
                            place: place,
                            address: mapPlaceToAddress(place),
                            suggestionDescription: item.value,
                            position: place.geometry?.location ? { lat: place.geometry.location.lat(), lng: place.geometry.location.lng() } : undefined
                        });
                    }
                } else {
                    refreshSessionToken();
                    setApiError(status);
                    if (onAPIError) onAPIError(status);
                }
            }
        });

    }, [getAutocompleteDetails, mapPlaceToAddress, onAPIError, onAddressFound, refreshSessionToken]);

    const handleOnItemSubmit = useCallback((item: AutocompleteItem & { place_id: string }) => {
        handleItemSelected(item);

    }, [handleItemSelected]);

    // MMC: Query if there is an initla value
    useEffect(() => {
        if (value) {
            setQuery(value);
        }
    }, []);

    //this makes the effect depends of debounce if not causes race conditions where sometimes the predictions are not updated when the user enters characters
    useEffect(() => {
        if (onChangeRef.current) onChangeRef.current(query);
    }, [query]);

    // MMC: update inputValue when value changes (sync)
    useEffect(() => {
        setInputValue(value || '');
    }, [value]);


    //TODO: Loading?
    useEffect(() => {

        if (!query) return;

        if (!query) {
            setSuggestions([]);
            return;
        }

        getAutocompletePredictions({
            searchText: query, restrictions: restrictions, callback: (predictions, status) => {
                if (status === google.maps.places.PlacesServiceStatus.OK) {
                    if (predictions?.length) {
                        setSuggestions(predictions.map(p => ({
                            place_id: p.place_id,
                            structuredFormatting: p.structured_formatting,
                            value: p.description,
                            icon: icon
                        })));
                    }
                }
                else {
                    if (status !== google.maps.places.PlacesServiceStatus.NOT_FOUND && status !== google.maps.places.PlacesServiceStatus.ZERO_RESULTS) {
                        setApiError(status);
                        if (onAPIError) onAPIError(status);
                    }
                }
            }
        });

    }, [getAutocompletePredictions, onAPIError, query, restrictions, icon]);


    return {
        userInputRef,
        attributionsRef,
        isLoading,
        handleOnUserInputChange,
        handleOnItemSubmit,
        suggestions,
        value: inputValue,
        apiError,
    };
}
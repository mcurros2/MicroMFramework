import { Text } from "@mantine/core";
import { ModalSettings } from "@mantine/modals/lib/context";
import { useRef } from "react";
import { latLng, useModal } from "../Core";
import { AddressFoundResult } from "../GoogleMaps";
import { AddressMappingRule } from "../GoogleMaps/Mapping.types";
import { AddressMapMarkerPosition } from "./AddressMapMarker";
import { AddressSearchForm } from "./AddressSearchForm";

export interface AddressSearchModalOptions {
    onOK: (address?: AddressFoundResult, position?: latLng) => void,
    onCancel?: () => void,
    onClosed?: () => void,
    modalProps?: ModalSettings,
    search?: string,
    helpMessage?: string,
    title?: string,
    countries: string[],
    currentPosition?: AddressMapMarkerPosition,
    mappingRules?: AddressMappingRule[]
}

export const UseAddressSearchFormDefaultProps: Partial<AddressSearchModalOptions> = {
    modalProps: { size: 'xl' },
    title: 'Address Search',
    helpMessage: 'Enter the address to search for. You can move the Marker to another position. Click OK when ready',
    mappingRules: []
}
export function useAddressSearchForm() {
    const modals = useModal();
    const buttonResult = useRef<'OK' | 'Cancel' | 'Quit'>('Quit');

    const open = async (props: AddressSearchModalOptions) => {
        const {
            onOK, onCancel, modalProps, search, helpMessage, onClosed, countries, title, currentPosition, mappingRules
        } = { ...UseAddressSearchFormDefaultProps, ...props };

        buttonResult.current = 'Quit';

        const handleOK = async (address?: AddressFoundResult, position?: latLng) => {
            buttonResult.current = 'OK';
            await modals.close();
            if (onOK) {
                await onOK(address, position);
            }
        };

        const handleCancel = async () => {
            buttonResult.current = 'Cancel';
            await modals.close();
            if (onCancel) {
                onCancel();
            }
        };

        const handleClosed = async () => {
            if (buttonResult.current === 'Quit') {
                if (onCancel) {
                    onCancel();
                }
            }
            if (onClosed) onClosed();
        }

        await modals.open(
            {
                content: <AddressSearchForm
                    countries={countries}
                    initialSearchAddress={search}
                    currentPosition={currentPosition}
                    helpMessage={helpMessage}
                    onOK={(address?: AddressFoundResult, position?: latLng) => handleOK(address, position)}
                    onCancel={() => handleCancel()}
                    mappingRules={mappingRules}
                />,
                modalProps: {
                    ...modalProps,
                    trapFocus: true,
                    returnFocus: true,
                    title: <Text fw="700">{title}</Text>,
                },
                onClosed: async () => {
                    await handleClosed();
                }

            });

    };

    return open;

}
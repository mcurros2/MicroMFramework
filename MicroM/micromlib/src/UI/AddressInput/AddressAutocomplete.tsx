import { ReactNode, useCallback, useEffect } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { Value } from "../../client";
import { latLng } from "../Core";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { AddressFoundResult, GoogleAddressAutocomplete, GoogleAddressAutocompleteProps, GoogleAddressAutocompleteRestrictions, MappedAddressResult } from "../GoogleMaps";

export interface AddressAutocompleteResult {
    address?: MappedAddressResult,
    position?: latLng,
    utcOffsetMinutes?: number,
    formattedAddress?: string,
}

export interface AddressAutocompleteProps extends Omit<GoogleAddressAutocompleteProps, 'restrictions' | 'onAddressFound'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: boolean

    countries: string[],

    onAddressFound?: (addressResult?: AddressAutocompleteResult) => void,
}

export function AddressAutocomplete(props: AddressAutocompleteProps) {
    //const { } = useComponentDefaultProps('AddressAutocomplete', {}, props);
    const {
        column, entityForm, validate, requiredMessage, validationContainer, countries, required, readOnly, onAddressFound,
        withAsterisk, label, placeholder, description, maxLength, autoFocus,
        ...rest
    } = props;

    const { status, form } = entityForm;

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    const restrictions: GoogleAddressAutocompleteRestrictions = {
        country: countries
    };

    const handleOnAddressFound = useCallback((result: AddressFoundResult) => {
        form.setFieldValue(column.name, result.suggestionDescription || result?.place.formatted_address || '');
        if (onAddressFound) {
            const found: AddressAutocompleteResult = {
                address: result.address,
                position: result.position,
                utcOffsetMinutes: result.place.utc_offset_minutes,
                formattedAddress: result.suggestionDescription || result.place.formatted_address
            };
            onAddressFound(found);
        }
    }, [column.name, form, onAddressFound]);

    useEffect(() => {
        form.setFieldValue(column.name, column.value || '');
    }, []);

    //useEffect(() => {
    //    if (!status.loading && status.operationType === 'get') {
    //        form.setFieldValue(column.name, column.value);
    //    }
    //}, [status.loading, status.operationType]);

    return (
        <GoogleAddressAutocomplete
            {...rest}

            restrictions={restrictions}
            onAddressFound={handleOnAddressFound}

            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            maxLength={maxLength ?? column.length}
            readOnly={entityForm.formMode === 'view' ? true : readOnly}

            autoFocus={autoFocus}
            data-autofocus={autoFocus}

            {...entityForm.form.getInputProps(column.name)}

        />
    )
}
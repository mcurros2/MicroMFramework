import { Stack } from "@mantine/core";
import { useCallback, useRef, useState } from "react";
import { EntityForm, MicroMClient, TextField, ValuesObject, latLng, useEntityForm } from "../../src";
import { AddressLookup } from "../../src/UI/AddressInput/AddressLookup";
import { AddressInputTestEntity } from "./AddressTestEntity";
import { useGoogleAddressMappingRules } from "../../src/UI/GoogleMaps/useGoogleAddressMappingRules";
import { ARMappingRules, UYMappingRules } from "../../src/UI/GoogleMaps/mappingRulesDefinition";
import { AddressAutocompleteResult } from "../../src/UI/AddressInput/AddressAutocomplete";


export function AddressLookupTest() {
    const client = useRef(new MicroMClient({app_id: "", api_url: ""}));
    const entity = useRef(new AddressInputTestEntity(client.current));

    const entityForm = useEntityForm({ entity: entity.current, initialFormMode: "add", getDataOnInit: false });

    const [currentPosition, setCurrentPosition] = useState<latLng>();

    const { mappingRules, addMappingRule } = useGoogleAddressMappingRules();

    addMappingRule([ARMappingRules, UYMappingRules]);

    // mapping here should involve creating countries, cities, etc.
    // as not every address table will have the same columns, this should be application specific
    // also we may need to set column vlues, as maybe not al fields are visible in the form
    const handleAddressFound = useCallback((result?: AddressAutocompleteResult) => {
        const values: ValuesObject = {};

        const mapped = result?.address;
        if (mapped) {
            values.vc_country = mapped?.country || '';
            values.vc_country_code = mapped?.countryCode || '';
            values.vc_street = mapped?.street || '';
            values.vc_street_number = mapped?.streetNumber || '';
            values.vc_province = mapped?.province || '';
            values.vc_city = mapped?.city || '';
            values.vc_department = mapped?.department || '';
            values.vc_floor = mapped?.floor || '';
            values.vc_apartment = mapped?.apartment || '';
            values.vc_postal_code = mapped?.postalCode || '';
        }

        if (result?.utcOffsetMinutes) values.vc_utc_offset_minutes = result.utcOffsetMinutes.toString();

        if (result?.position) {
            values.vc_latitude = result.position.lat.toString();
            values.vc_longitude = result.position.lng.toString();
            setCurrentPosition(result.position);
        }

        entityForm.form.setValues(values);

    }, [entityForm.form]);

    return (
        <EntityForm formAPI={entityForm}>
            <Stack>
                <AddressLookup
                    entityForm={entityForm}
                    column={entity.current.def.columns.vc_references}
                    currentPosition={currentPosition}
                    countries={["ar", "uy", "cl"]}
                    onAddressFound={handleAddressFound}
                    label="Domicilio"
                    mappingRules={mappingRules}
                />
                <TextField
                    column={entity.current.def.columns.vc_street}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_street_number}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_apartment}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_province}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_city}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_department}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_country}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_postal_code}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_latitude}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_longitude}
                    entityForm={entityForm}
                />
                <TextField
                    column={entity.current.def.columns.vc_utc_offset_minutes}
                    entityForm={entityForm}
                />
            </Stack>
        </EntityForm>

    );
}
import { AddressAutocomplete, TextField, useEntityForm } from "UI";
import { MicroMClient } from "client";
import { useRef } from "react";
import { AddressInputTestEntity } from "./AddressTestEntity";
import { Stack } from "@mantine/core";

export function AddressInputTest() {

    const client = useRef(new MicroMClient({ app_id: "", api_url: "" }));
    const entity = useRef(new AddressInputTestEntity(client.current));

    const entityForm = useEntityForm({ entity: entity.current, initialFormMode: "add", getDataOnInit: false });

    return <Stack>
        <AddressAutocomplete
            autoFocus
            entityForm={entityForm}
            column={entity.current.def.columns.vc_references}
            countries={['UY', 'AR']}
            onAddressFound={(result) => {
                entityForm.form.setValues({
                    vc_country: result?.address?.country || '',
                    vc_country_code: result?.address?.countryCode || '',
                    vc_street: result?.address?.street || '',
                    vc_street_number: result?.address?.streetNumber || '',
                    vc_province: result?.address?.province || '',
                    vc_city: result?.address?.city || '',
                    vc_department: result?.address?.department || '',
                    vc_floor: result?.address?.floor || '',
                    vc_apartment: result?.address?.apartment || '',
                    vc_postal_code: result?.address?.postalCode || '',
                    vc_utc_offset_minutes: result?.utcOffsetMinutes?.toString() || '',
                    vc_latitude: result?.position?.lat.toString() || '',
                    vc_longitude: result?.position?.lng.toString() || ''
                });
                console.log('Address found:', entityForm.form.values);
            }}
        />
        <TextField
            entityForm={entityForm}
            column={entity.current.def.columns.vc_webinsuser}
            validate={{
                custom: {
                    data: (value: unknown, values: Record<string, unknown>) => {
                        if (!values.vc_country) {
                            return 'No Country';
                        }
                        return null;
                    }
                }
            }}
        />
    </Stack>
}

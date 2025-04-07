import { Stack, Text } from "@mantine/core";
import { useRef } from "react";
import { EntityForm, MicroMClient, useEntityForm } from "../../src";
import { RingProgressField } from "../../src/UI/Stats";
import { RingProgressFieldEntity } from "./RingProgressFieldEntity";
import { IconUser } from "@tabler/icons-react";




export function RingProgressFieldTest() {

    const client = useRef(new MicroMClient({ api_url: '', app_id: '' }));
    const entity = useRef(new RingProgressFieldEntity(client.current));

    const entityForm = useEntityForm({ entity: entity.current, initialFormMode: "add", getDataOnInit: false });

    const cols = entity.current.def.columns;

    const maxValue = cols.i_ausentes.value + cols.i_presentes.value + cols.i_demorados.value;


    return (
        <EntityForm formAPI={entityForm}>
            <Stack>
                <RingProgressField
                    column={cols.i_presentes}
                    maxValue={maxValue}
                    displayPercent="percent"
                    centerIcon={IconUser}
                />
                <RingProgressField
                    column={cols.i_ausentes}
                    maxValue={maxValue}
                    displayPercent="fraction"
                    showPercentAsCenterLabel
                />
                <RingProgressField
                    column={cols.i_demorados}
                    maxValue={maxValue}
                    displayPercent="none"
                    centerLabel={<Text align="center" weight="bold" size="xs">D</Text>}
                />
            </Stack>
        </EntityForm>

    )
}
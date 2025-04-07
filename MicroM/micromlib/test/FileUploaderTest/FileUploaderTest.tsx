import { Button, Card, Group, Stack } from "@mantine/core";
import { DatesProvider } from "@mantine/dates";
import { EntityForm, FilesUploadForm, NotifyError, NumberField, UseLocaleFormatDefaultProps, WeekPickerField, useEntityForm, useModal } from "UI";
import { MicroMClient } from "client";
import 'dayjs/locale/es';
import { useRef } from "react";
import { FileUploaderTestEntity } from "./FileUploaderTestEntity";

UseLocaleFormatDefaultProps.initialLocale = 'es-AR';

export function FileUploaderTest() {

    const client = useRef(new MicroMClient({ api_url: '', app_id: '' }));
    const entity = useRef(new FileUploaderTestEntity(client.current));

    const entityForm = useEntityForm({ entity: entity.current, initialFormMode: "add", getDataOnInit: false });

    const modal = useModal();

    return (
        <DatesProvider settings={{ locale: 'es' }}>
            <EntityForm formAPI={entityForm}>
                <Card>
                    <Stack>
                        <FilesUploadForm
                            client={client.current}
                            fileProcessColumn={entity.current.def.columns.c_fileprocess_id}
                            maxFilesCount={2}
                            editor="image" />
                        <NumberField entityForm={entityForm} column={entity.current.def.columns.i_order} />
                        <WeekPickerField
                            entityForm={entityForm}
                            weekStartDateColumn={entity.current.def.columns.d_week_date}
                        />
                        <Button onClick={
                            async () => {
                                modal.open({
                                    modalProps: {
                                        title: 'Test',
                                        size: 'md',
                                    },
                                    content: <Card>
                                        <Group grow>
                                            <NotifyError withCloseButton title="">Test error</NotifyError>
                                        </Group>
                                    </Card>
                                })
                            }
                        }>Notify error</Button>
                    </Stack>
                </Card>
            </EntityForm>
        </DatesProvider>
    )
}

import { Group, Stack, useComponentDefaultProps } from "@mantine/core";
import { CheckboxField, EntityForm, FormOptions, LookupSelect, NotifyBitField, NotifyBitFieldDefaultProps, PasswordField, RingProgressField, TextField, useEntityForm } from "../../UI";
import { MicromUsers } from "./MicromUsers";


export interface MicromUsersFormProps extends FormOptions<MicromUsers> {
    disabledTrueLabel?: string,
    disabledFalseLabel?: string,
    lockedRemainingLabel?: string,
    userEnabledLabel?: string,
    userDisabledLabel?: string,
    logonAttemptsStatusTitle?: string
    logonAttemptsStatusDescription?: string,
    lockedTitleLabel?: string,
    minutesLabel?: string,
    willUnlockInNextLogonLabel?: string
}

export const MicromUsersFormDefaultProps: Partial<MicromUsersFormProps> = {
    initialFormMode: 'view',
    disabledFalseLabel: 'The account is enabled',
    disabledTrueLabel: 'The account is disabled',
    lockedRemainingLabel: 'The account is locked. It will unlock automatically in',
    lockedTitleLabel: 'The account is locked',
    userEnabledLabel: 'The user is enabled',
    userDisabledLabel: 'The user is disabled',
    logonAttemptsStatusTitle: 'Bad login attemps',
    logonAttemptsStatusDescription: 'The account registers failed login attempts',
    minutesLabel: 'minutes',
    willUnlockInNextLogonLabel: 'Account automatic lock period has finished. It will be unlocked in the next successful logon.'
}

export function MicromUsersForm(props: MicromUsersFormProps) {
    const {
        entity, initialFormMode, getDataOnInit, onSaved, onCancel, disabledFalseLabel, disabledTrueLabel,
        lockedRemainingLabel, userDisabledLabel, userEnabledLabel, logonAttemptsStatusDescription, logonAttemptsStatusTitle, lockedTitleLabel,
        minutesLabel, willUnlockInNextLogonLabel
    } = useComponentDefaultProps('MicromUsersForm', MicromUsersFormDefaultProps, props);

    const entityForm = useEntityForm(
        {
            entity: entity,
            initialFormMode: initialFormMode,
            validateInputOnBlur: true,
            getDataOnInit: getDataOnInit!,
            onSaved: onSaved,
            onCancel: onCancel
        }
    );

    const { formMode, status } = entityForm;

    const MAXBADLOGON_ATTEMPTS = 10;

    return (
        <EntityForm formAPI={entityForm}>
            <Stack>
                {formMode !== 'add' &&
                    <TextField entityForm={entityForm} column={entity.def.columns.c_user_id} maw="20rem" readOnly />
                }
                <LookupSelect
                    entityForm={entityForm}
                    formStatus={status}
                    entity={entity}
                    parentKeys={{}}
                    column={entity.def.columns.c_usertype_id}
                    lookupDefName={entity.def.lookups.UserTypes.name}
                    selectProps={{
                        searchable: false,
                        allowDeselect: true,
                        w: "30rem",
                        required: true,
                        withAsterisk: true
                    }}
                />
                <TextField entityForm={entityForm} column={entity.def.columns.vc_username} maw="25rem" />
                <TextField entityForm={entityForm} column={entity.def.columns.vc_email} required={false} />
                {formMode == 'add' &&
                    <PasswordField entityForm={entityForm} column={entity.def.columns.vc_password} />
                }
                <CheckboxField entityForm={entityForm} column={entity.def.columns.bt_disabled} required={false} />
                <Group>
                    {(!entity.def.columns.bt_islocked.value || entity.def.columns.bt_disabled.value) &&
                        <NotifyBitField
                            column={entity.def.columns.bt_disabled}
                            title={entity.def.columns.bt_disabled.value ? userDisabledLabel : userEnabledLabel}
                            trueMessage={disabledTrueLabel}
                            falseMessage={disabledFalseLabel}
                            withBorder
                            trueColor={NotifyBitFieldDefaultProps.falseColor}
                            trueIcon={NotifyBitFieldDefaultProps.falseIcon}
                            falseColor={NotifyBitFieldDefaultProps.trueColor}
                            falseIcon={NotifyBitFieldDefaultProps.trueIcon}
                        />
                    }
                    {entity.def.columns.i_badlogonattempts.value > 0 && !entity.def.columns.bt_islocked.value && entity.def.columns.i_locked_minutes_remaining.value === 0 &&
                        <RingProgressField
                            column={entity.def.columns.i_badlogonattempts}
                            maxValue={MAXBADLOGON_ATTEMPTS}
                            color="red"
                            title={logonAttemptsStatusTitle ?? entity.def.columns.i_badlogonattempts.prompt}
                            description={logonAttemptsStatusDescription ?? entity.def.columns.i_badlogonattempts.description}
                            maw="25rem"
                            displayPercent="fraction"
                        />
                    }
                    {entity.def.columns.bt_islocked.value &&
                        <NotifyBitField
                            column={entity.def.columns.bt_islocked}
                            title={lockedTitleLabel}
                            trueMessage={entity.def.columns.i_locked_minutes_remaining.value > 0 ? `${lockedRemainingLabel} ${entity.def.columns.i_locked_minutes_remaining.value} ${minutesLabel}` : willUnlockInNextLogonLabel}
                            falseMessage={disabledFalseLabel}
                            withBorder
                            trueColor={NotifyBitFieldDefaultProps.falseColor}
                            trueIcon={NotifyBitFieldDefaultProps.falseIcon}
                            falseColor={NotifyBitFieldDefaultProps.trueColor}
                            falseIcon={NotifyBitFieldDefaultProps.trueIcon}
                        />
                    }
                </Group>
            </Stack>
        </EntityForm>
    )

}
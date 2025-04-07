import { Group, Stack, Text, useComponentDefaultProps } from "@mantine/core";
import { NotifyBitField, NotifyBitFieldDefaultProps, NotifyInfo, RingProgressField, UseEntityFormReturnType } from "../../UI";
import { IMicromUserStatusPanelColums } from "./MicromUsersStatusPanelColumns";

export interface MicromUsersStatusPanel {
    columns: IMicromUserStatusPanelColums,
    maxBadLogonAttemps: number,
    entityForm: UseEntityFormReturnType
    disabledTrueLabel?: string,
    disabledFalseLabel?: string,
    lockedRemainingLabel?: string,
    userEnabledLabel?: string,
    userDisabledLabel?: string,
    logonAttemptsStatusTitle?: string
    logonAttemptsStatusDescription?: string,
    lockedTitleLabel?: string,
    minutesLabel?: string,
    willUnlockInNextLogonLabel?: string,
    loginInfoTitle?: string,
}

export const MicromUsersStatusPanelDefaultProps: Partial<MicromUsersStatusPanel> = {
    disabledFalseLabel: 'The account is enabled',
    disabledTrueLabel: 'The account is disabled',
    lockedRemainingLabel: 'The account is locked. It will unlock automatically in',
    lockedTitleLabel: 'The account is locked',
    userEnabledLabel: 'The user is enabled',
    userDisabledLabel: 'The user is disabled',
    logonAttemptsStatusTitle: 'Bad login attemps',
    logonAttemptsStatusDescription: 'The account registers failed login attempts',
    minutesLabel: 'minutes',
    willUnlockInNextLogonLabel: 'Account automatic lock period has finished. It will be unlocked in the next successful logon.',
    loginInfoTitle: 'Login information'
}

export function MicromMUsersStatusPanel(props: MicromUsersStatusPanel) {

    const {
        columns, userDisabledLabel, userEnabledLabel, disabledTrueLabel, disabledFalseLabel, entityForm, maxBadLogonAttemps,
        logonAttemptsStatusTitle, logonAttemptsStatusDescription, lockedTitleLabel, minutesLabel, willUnlockInNextLogonLabel,
        lockedRemainingLabel, loginInfoTitle
    } = useComponentDefaultProps('MicromMUsersStatusPanel', MicromUsersStatusPanelDefaultProps, props);


    return (
        <Stack>
            <Group>
                <NotifyInfo title={loginInfoTitle} withBorder>
                    <Text size="xs" color="dimmed">
                        {columns.dt_last_login.value ? `Last login: ${columns.dt_last_login.value.toLocaleString()}` : 'No login registered'}
                        {columns.dt_last_refresh.value ? `, Last refresh: ${columns.dt_last_refresh.value.toLocaleString()}` : ', No refresh registered'}
                    </Text>
                </NotifyInfo>
                {(!columns.bt_islocked.value || columns.bt_disabled.value) &&
                    <NotifyBitField
                        column={columns.bt_disabled}
                        title={columns.bt_disabled.value ? userDisabledLabel : userEnabledLabel}
                        trueMessage={disabledTrueLabel}
                        falseMessage={disabledFalseLabel}
                        withBorder
                        trueColor={NotifyBitFieldDefaultProps.falseColor}
                        trueIcon={NotifyBitFieldDefaultProps.falseIcon}
                        falseColor={NotifyBitFieldDefaultProps.trueColor}
                        falseIcon={NotifyBitFieldDefaultProps.trueIcon}
                    />
                }
                {columns.i_badlogonattempts.value > 0 && !columns.bt_islocked.value && columns.i_locked_minutes_remaining.value === 0 &&
                    <RingProgressField
                        column={columns.i_badlogonattempts}
                        maxValue={maxBadLogonAttemps}
                        color="red"
                        title={logonAttemptsStatusTitle ?? columns.i_badlogonattempts.prompt}
                        description={logonAttemptsStatusDescription ?? columns.i_badlogonattempts.description}
                        maw="25rem"
                        displayPercent="fraction"
                    />
                }
                {columns.bt_islocked.value &&
                    <NotifyBitField
                        column={columns.bt_islocked}
                        title={lockedTitleLabel}
                        trueMessage={columns.i_locked_minutes_remaining.value > 0 ? `${lockedRemainingLabel} ${columns.i_locked_minutes_remaining.value} ${minutesLabel}` : willUnlockInNextLogonLabel}
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
    )
}
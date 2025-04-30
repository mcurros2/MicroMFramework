import { ActionIcon, Button, DefaultMantineColor, FocusTrap, Group, Notification, ScrollArea, Space, Variants, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCircleCheck, IconCircleX, IconHelp, IconHelpOff, IconX } from "@tabler/icons-react";
import { PropsWithChildren, ReactNode, useEffect } from "react";
import { AlertError, FakeProgressBar, usePreventEnterSubmission } from "../Core";
import { UseEntityFormReturnType } from "./useEntityForm";

export interface EntityFormProps extends PropsWithChildren {
    showOK?: boolean,
    showCancel?: boolean,
    showFormValidationNotification?: boolean,
    showLoadingProgress?: boolean,
    showErrors?: boolean,
    OKText?: ReactNode,
    CancelText?: ReactNode,
    CloseText?: ReactNode,
    formAPI: UseEntityFormReturnType,
    invalidFieldsLabel?: string,
    showHelpButton?: boolean,
    initialShowDescriptionInFields?: boolean,
    preventEnterSubmission?: boolean,
    buttons?: ReactNode,
    saveAndGetOnSubmit?: boolean,
    isDirtyColor?: DefaultMantineColor,
    cancelButtonVariant?: Variants<'filled' | 'outline' | 'light' | 'white' | 'default' | 'subtle' | 'gradient'>,
    okButtonVariant?: Variants<'filled' | 'outline' | 'light' | 'white' | 'default' | 'subtle' | 'gradient'>,
    saveBeforeLocalNavigation?: boolean,
    saveBeforeRemoteNavigation?: boolean,
}

export const EntityFormDefaultProps: Partial<EntityFormProps> = {
    showOK: true,
    showCancel: true,
    showFormValidationNotification: true,
    showLoadingProgress: true,
    showErrors: true,
    OKText: "Save",
    CancelText: "Cancel",
    CloseText: "Close",
    invalidFieldsLabel: "Some fields are invalid, please review the form",
    showHelpButton: false,
    preventEnterSubmission: true,
    isDirtyColor: 'green',
    cancelButtonVariant: 'light',
    okButtonVariant: 'filled',
}

export function EntityForm(props: EntityFormProps) {
    const {
        formAPI, children, showOK, showCancel, showErrors, showFormValidationNotification, showLoadingProgress, OKText, CancelText, invalidFieldsLabel,
        showHelpButton, preventEnterSubmission, CloseText, buttons, isDirtyColor, cancelButtonVariant, okButtonVariant
    } = useComponentDefaultProps('EntityForm', EntityFormDefaultProps, props);

    const { entity } = formAPI;

    const theme = useMantineTheme();

    const { handleCancel, handleSubmit, notifyValidationErrorState, status, form, formMode, showDescriptionState, isFormValid, asyncErrors } = formAPI;
    const [notifyValidationError, setNotifyValidationError] = notifyValidationErrorState;
    const [showDescription, setShowDescription] = showDescriptionState;

    // Closes the error notification when the form becomes valid
    useEffect(() => {
        if (isFormValid()) setNotifyValidationError(false);
    }, [isFormValid, setNotifyValidationError]);

    const handleKeyDown = usePreventEnterSubmission();

    // MMC: FocusTrap is present here because the FocusTrap that uses breaks with children when is an array
    // and because when we edit/view the form, when performing get the fields are denied and can't be focused
    // be ware of queryStatus.loading === false as the initial queryStatus is {}, this is on purpose to avoid disabling the fields before loading data

    return (
        <>
            {showHelpButton &&
                <Group mb="xs">
                    <ActionIcon color={theme.primaryColor} radius="xl" variant="light" onClick={() => setShowDescription((prev) => !prev)}>{showDescription ? <IconHelpOff size="1.2rem" /> : <IconHelp size="1.2rem" />}</ActionIcon>
                </Group>
            }
            <FocusTrap active={status.loading === false}>
                <form onSubmit={handleSubmit} onKeyDown={preventEnterSubmission ? handleKeyDown : undefined} onInvalid={() => setNotifyValidationError(true)}>
                    {showLoadingProgress && status.loading && <FakeProgressBar size="xs" />}
                    {showLoadingProgress && !status.loading && <Space h="0.1875rem" />}
                    {showFormValidationNotification && notifyValidationError &&
                        <Notification icon={<IconX size="1.1rem" />} color="red" onClose={() => setNotifyValidationError(false)}>
                            {`${invalidFieldsLabel}: ${Object.keys({ ...form.errors, ...asyncErrors }).map(formKey => `[${entity.def.columns[formKey].prompt}]`).join(', ')}`}
                        </Notification>
                    }
                    <fieldset disabled={status.loading} style={{ borderWidth: 0, margin: 0, padding: 0, minInlineSize: 'unset' }}>
                        {children}
                    </fieldset>
                    {showErrors && status.error &&
                        <AlertError mt="md" iconTooltip={`Code #${status.error.status}`}>{status.error.message} {status.error.statusMessage ?? ''}</AlertError>
                    }
                    <Group mt="md">
                        {buttons}
                        {(showOK || showCancel) &&
                            <Group position="right" style={{ flex: 'auto' }}>
                                {showCancel && <Button variant={cancelButtonVariant} leftIcon={<IconCircleX size="1.125rem" />} onClick={handleCancel} >{formMode === 'view' ? CloseText : CancelText}</Button>}
                                {showOK && formMode != "view" && <Button variant={okButtonVariant} type="submit" color={form.isDirty() ? isDirtyColor : theme.primaryColor} loading={status?.loading} leftIcon={<IconCircleCheck size="1.125rem" />}>{OKText}</Button>}
                            </Group>
                        }
                    </Group>
                </form>
            </FocusTrap>
        </>
    )

}
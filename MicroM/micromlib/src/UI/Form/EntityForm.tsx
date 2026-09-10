import { ActionIcon, Button, DefaultMantineColor, FocusTrap, Group, Notification, Space, useComponentDefaultProps, useMantineTheme, Variants } from "@mantine/core";
import { IconCircleCheck, IconCircleX, IconHelp, IconHelpOff, IconX } from "@tabler/icons-react";
import { FormEvent, PropsWithChildren, ReactNode, useEffect, useRef } from "react";
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
    disableOKIfNotDirty?: boolean,
    formHeight?: string | number,
    disableOK?: boolean,
    loadingOK?: boolean,
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
    formHeight: '100%',
}

export function EntityForm(props: EntityFormProps) {
    const explicitOKText = props.OKText;
    const explicitCancelText = props.CancelText;

    const {
        formAPI, children, showOK, showCancel, showErrors, showFormValidationNotification, showLoadingProgress, OKText, CancelText, invalidFieldsLabel,
        showHelpButton, preventEnterSubmission, CloseText, buttons, isDirtyColor, cancelButtonVariant, okButtonVariant, disableOKIfNotDirty,
        formHeight, disableOK, loadingOK
    } = useComponentDefaultProps('EntityForm', EntityFormDefaultProps, props);

    const { entity } = formAPI;

    const theme = useMantineTheme();

    const {
        handleCancel, handleSubmit, notifyValidationErrorState, status, form, formMode,
        showDescriptionState, isFormValid, asyncErrors, activeFormActions,
    } = formAPI;

    const [notifyValidationError, setNotifyValidationError] = notifyValidationErrorState;
    const [showDescription, setShowDescription] = showDescriptionState;

    // Closes the error notification when the form becomes valid
    useEffect(() => {
        if (isFormValid()) setNotifyValidationError(false);
    }, [isFormValid, setNotifyValidationError]);

    const handleKeyDown = usePreventEnterSubmission();

    const okElement = useRef<HTMLButtonElement>(null);
    const cancelElement = useRef<HTMLButtonElement>(null);

    const effectiveOKText = activeFormActions.OK && explicitOKText === undefined
        ? activeFormActions.OK.label ?? OKText
        : OKText;

    const defaultCancelText = formMode === 'view' ? CloseText : CancelText;

    const effectiveCancelText = activeFormActions.Cancel
        ? explicitCancelText !== undefined
            ? explicitCancelText
            : activeFormActions.Cancel.label ?? defaultCancelText
        : defaultCancelText;

    const handleFormSubmit = (event: FormEvent<HTMLFormElement>) => {
        void handleSubmit(event, false, okElement.current ?? undefined);
    };

    const handleCancelClick = () => {
        void handleCancel(false, cancelElement.current ?? undefined);
    };

    // MMC: FocusTrap is present here because the FocusTrap that uses breaks with children when is an array
    // and because when we edit/view the form, when performing get the fields are denied and can't be focused
    // be ware of queryStatus.loading === false as the initial queryStatus is {}, this is on purpose to avoid disabling the fields before loading data

    const fieldsetHeight = formHeight === '100%' ? 'calc(100% - 3rem)' : 'unset';

    return (
        <>
            {showHelpButton &&
                <Group mb="xs">
                    <ActionIcon color={theme.primaryColor} radius="xl" variant="light" onClick={() => setShowDescription((prev) => !prev)}>{showDescription ? <IconHelpOff size="1.2rem" /> : <IconHelp size="1.2rem" />}</ActionIcon>
                </Group>
            }
            <FocusTrap active={status.loading === false}>
                <form onSubmit={handleFormSubmit} onKeyDown={preventEnterSubmission ? handleKeyDown : undefined} style={{ height: formHeight }} onInvalid={() => setNotifyValidationError(true)}>
                    {showLoadingProgress && status.loading && <FakeProgressBar size="xs" />}
                    {showLoadingProgress && !status.loading && <Space h="0.1875rem" />}
                    {showFormValidationNotification && notifyValidationError &&
                        <Notification icon={<IconX size="1.1rem" />} color="red" onClose={() => setNotifyValidationError(false)}>
                            {`${invalidFieldsLabel}: ${Object.keys({ ...form.errors, ...asyncErrors }).map(formKey => `[${entity.def.columns[formKey].prompt}]`).join(', ')}`}
                        </Notification>
                    }
                    <fieldset disabled={status.loading} style={{ borderWidth: 0, margin: 0, padding: 0, minInlineSize: 'unset', height: fieldsetHeight }}>
                        {children}
                    </fieldset>
                    {showErrors && status.error &&
                        <AlertError mt="md" iconTooltip={`Code #${status.error.status}`}>{status.error.message} {status.error.statusMessage ?? ''}</AlertError>
                    }
                    <Group mt="md">
                        {buttons}
                        {(showOK || showCancel) &&
                            <Group position="right" style={{ flex: 'auto' }}>
                                {showCancel && <Button ref={cancelElement} type="button" variant={cancelButtonVariant} leftIcon={<IconCircleX size="1.125rem" />} onClick={handleCancelClick} >{effectiveCancelText}</Button>}
                                {showOK && formMode != "view" && <Button ref={okElement} variant={okButtonVariant} type="submit" color={form.isDirty() ? isDirtyColor : theme.primaryColor} loading={loadingOK || status?.loading} disabled={disableOK === true || (disableOKIfNotDirty && !form.isDirty())} leftIcon={<IconCircleCheck size="1.125rem" />}>{effectiveOKText}</Button>}
                            </Group>
                        }
                    </Group>
                </form>
            </FocusTrap>
        </>
    )

}

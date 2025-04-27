import { Button, Group, Stepper, StepperProps, useComponentDefaultProps } from "@mantine/core";
import { IconCircleCheck } from "@tabler/icons-react";
import { ReactNode, useCallback, useEffect, useMemo, useState } from "react";
import { DBStatusResult } from "../../client";
import { EntityForm, EntityFormProps } from "./EntityForm";

export interface StepperFormStep {
    name: string,
    label: string,
    description?: string,
    validateFields?: string[],
    content: ReactNode,
    nextStepLabel?: string,
    nextStepValidLabel?: string,
    nextStepValidation?: () => boolean | Promise<boolean>,
    icon?: ReactNode,
}

export interface StepperFormProps extends EntityFormProps {
    steps: StepperFormStep[],
    onNextStep?: (step: StepperFormStep) => boolean | Promise<boolean>,
    onPrevStep?: (step: StepperFormStep) => boolean | Promise<boolean>,
    initialStep: number,
    nextStepLabel?: string,
    prevStepLabel?: string,
    allowStepClickForward?: boolean,
    completedContent?: ReactNode,
    hideNextAndBackWhenCompleted?: boolean
    onCompleted?: () => void,
    stepperProps?: Omit<Partial<StepperProps>, 'children' | 'active'>
}

export const StepperFormDefaultProps: Partial<StepperFormProps> = {
    nextStepLabel: "Next step",
    prevStepLabel: "Back",
    showOK: false,
    showCancel: false,
    showFormValidationNotification: true,
    showLoadingProgress: true,
    showErrors: true,
    OKText: "Save",
    CancelText: "Cancel",
    CloseText: "Close",
    invalidFieldsLabel: "Some fields are invalid, please review the form",
    showHelpButton: false,
    preventEnterSubmission: true,
    allowStepClickForward: false,
    hideNextAndBackWhenCompleted: true
};


export function StepperForm(props: StepperFormProps) {
    const {
        formAPI, onNextStep, onPrevStep, initialStep, nextStepLabel, prevStepLabel, allowStepClickForward,
        steps, OKText, completedContent, hideNextAndBackWhenCompleted, stepperProps, onCompleted, ...rest
    } = useComponentDefaultProps('StepperForm', StepperFormDefaultProps, props);

    const { status, formMode } = formAPI;

    const [activeStep, setActiveStep] = useState(initialStep);
    const [stepValidating, setStepValidating] = useState(false);
    const [stepValid, setStepValid] = useState<boolean[]>(Array(steps.length).fill(false));

    const activeStepItem = steps[activeStep];

    const validateCurrentStepFields = useCallback((): boolean => {
        const currentStep = steps[activeStep];
        if (currentStep.validateFields) {
            const validationResults = currentStep.validateFields.map((field) => formAPI.form.validateField(field));
            return validationResults.every((result) => result.hasError === false);
        }
        return true;
    }, [activeStep, formAPI.form, steps]);

    const nextStep = useCallback(async (event: React.MouseEvent) => {
        event.preventDefault();

        const currentStep = steps[activeStep];
        let isValid = validateCurrentStepFields();

        if (!isValid) {
            return;
        }

        const hasAsyncValidation = currentStep.nextStepValidation || onNextStep;

        if (hasAsyncValidation && !stepValid[activeStep]) {
            setStepValidating(true);
            try {
                if (currentStep.nextStepValidation) {
                    isValid = await currentStep.nextStepValidation();
                }
                if (isValid && onNextStep) {
                    isValid = await onNextStep(steps[activeStep + 1]);
                }
            } catch (e) {
                throw e;
            } finally {
                setStepValidating(false);
            }

            if (isValid) {
                setStepValid((prev) => {
                    const newStepValid = [...prev];
                    newStepValid[activeStep] = true;
                    return newStepValid;
                });

                if (activeStep < steps.length - 1) {
                    setActiveStep((current) => current + 1);
                } else {
                    if (formMode === 'view') setActiveStep(steps.length);  // Completed step
                }

            } else {
                setStepValid((prev) => {
                    const newStepValid = [...prev];
                    newStepValid[activeStep] = false;
                    return newStepValid;
                });
            }
        } else {
            if (activeStep < steps.length - 1) {
                setActiveStep((current) => current + 1);
            } else {
                if (formMode === 'view') setActiveStep(steps.length);  // Completed step
            }
        }
    }, [activeStep, formMode, onNextStep, stepValid, steps, validateCurrentStepFields]);

    const prevStep = useCallback(async (event: React.MouseEvent) => {
        event.preventDefault();
        let isValid = true;
        if (activeStep > 0) {
            try {
                if (onPrevStep) {
                    setStepValidating(true);
                    isValid = await onPrevStep(steps[activeStep - 1]);
                }
            } catch (e) {
                throw e;
            } finally {
                setStepValidating(false);
            }
            if (isValid) {
                setActiveStep((current) => current - 1);
            }
        }
    }, [activeStep, onPrevStep, steps]);

    const handleStepClick = useCallback(async (stepIndex: number) => {
        if (stepIndex < activeStep) {
            setActiveStep(stepIndex);
        } else if (allowStepClickForward) {
            const isValid = validateCurrentStepFields();
            if (isValid) {
                setActiveStep(stepIndex);
            }
        }
    }, [activeStep, allowStepClickForward, validateCurrentStepFields]);

    const buttons = useMemo(() => (
        !(hideNextAndBackWhenCompleted && activeStep === steps.length) &&
        <Group position={activeStep > 0 ? 'apart' : 'right'} style={{ flex: 'auto' }}>
            <Button
                key="stepper-back"
                loading={stepValidating}
                variant="default"
                type="button"
                onClick={prevStep}
                display={activeStep > 0 ? 'inline-block' : 'none'}
            >
                {prevStepLabel}
            </Button>
            <Button
                type="submit"
                key="stepper-submit"
                loading={status?.loading}
                leftIcon={<IconCircleCheck size="1.125rem" />}
                display={formMode !== "view" && activeStep === steps.length - 1 ? 'inline-block' : 'none'}
            >
                {activeStepItem.nextStepLabel || nextStepLabel}
            </Button>
            <Button
                loading={stepValidating}
                key="stepper-next"
                onClick={nextStep}
                type="button"
                display={activeStep < steps.length - 1 ? 'inline-block' : 'none'}
            >
                {(stepValid[activeStep]
                    ? activeStepItem.nextStepValidLabel
                    : activeStepItem.nextStepLabel) || nextStepLabel}
            </Button>
        </Group>
    ), [activeStep, activeStepItem, formMode, hideNextAndBackWhenCompleted, nextStep, nextStepLabel, prevStep, prevStepLabel, status?.loading, stepValid, stepValidating, steps.length]);

    useEffect(() => {
        setStepValid((prev) => {
            const newStepValid = [...prev];
            newStepValid[activeStep] = false;
            return newStepValid;
        });
    }, [activeStep]);

    useEffect(() => {
        if (!status.error && !status.loading && status.data) {
            const dbstat = status.data as DBStatusResult;
            if (!dbstat?.Failed) {
                setActiveStep(steps.length);  // Completed step
            }
        }
    }, [status.data, status.error, status.loading, steps.length]);

    useEffect(() => {
        if (activeStep === steps.length && onCompleted) {
            onCompleted();
        }
    }, [activeStep, onCompleted, steps.length]);

    return (
        <EntityForm {...rest} buttons={buttons} formAPI={formAPI} showCancel={false} showOK={false} OKText={OKText}>
            <Stepper active={activeStep} onStepClick={handleStepClick} {...stepperProps}>
                {steps.map((step) =>
                (
                    <Stepper.Step key={step.name} label={step.label} description={step.description} icon={step.icon}>
                        {step.content}
                    </Stepper.Step>
                )
                )}
                {completedContent &&
                    <Stepper.Completed>
                        {completedContent}
                    </Stepper.Completed>
                }
            </Stepper>
        </EntityForm>
    )
}
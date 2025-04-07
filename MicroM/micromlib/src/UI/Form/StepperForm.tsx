import { Button, Group, Stepper, StepperProps, useComponentDefaultProps } from "@mantine/core";
import { IconCircleCheck } from "@tabler/icons-react";
import { ReactNode, useEffect, useState } from "react";
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

    const validateCurrentStepFields = (): boolean => {
        const currentStep = steps[activeStep];
        if (currentStep.validateFields) {
            const validationResults = currentStep.validateFields.map((field) => formAPI.form.validateField(field));
            return validationResults.every((result) => result.hasError === false);
        }
        return true;
    };

    const nextStep = async (event: React.MouseEvent) => {
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
                    setActiveStep(steps.length);  // Completed step
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
                setActiveStep(steps.length);  // Completed step
            }

        }
    };

    const prevStep = async (event: React.MouseEvent) => {
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
    };

    const handleStepClick = async (stepIndex: number) => {
        if (stepIndex < activeStep) {
            setActiveStep(stepIndex);
        } else if (allowStepClickForward) {
            const isValid = validateCurrentStepFields();
            if (isValid) {
                setActiveStep(stepIndex);
            }
        }
    };

    useEffect(() => {
        setStepValid((prev) => {
            const newStepValid = [...prev];
            newStepValid[activeStep] = false;
            return newStepValid;
        });
    }, [formAPI.form.values, activeStep]);

    useEffect(() => {
        if (!status.error && !status.loading && status.data) {
            const dbstat = status.data as DBStatusResult;
            if (!dbstat?.Failed) {
                setActiveStep(steps.length);  // Completed step
            }
        }
    }, [status.data, status.error, status.loading, steps.length]);

    useEffect(() => {
        if(activeStep === steps.length && onCompleted) {
            onCompleted();
        }
    }, [activeStep, onCompleted, steps.length]);

    return (
        <EntityForm {...rest} formAPI={formAPI} showCancel={false} showOK={false} OKText={OKText}>
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
            {!(hideNextAndBackWhenCompleted && activeStep === steps.length) &&
                <Group position={activeStep > 0 ? 'apart' : 'right'} mt="xl">
                    {activeStep > 0 && (
                        <Button
                            loading={stepValidating}
                            variant="default"
                            onClick={prevStep}
                        >
                            {prevStepLabel}
                        </Button>
                    )}
                    {formMode !== "view" && activeStep === steps.length - 1 ? (
                        <Button
                            type="submit"
                            key="stepper-submit"
                            loading={status?.loading}
                            leftIcon={<IconCircleCheck size="1.125rem" />}
                        >
                            {activeStepItem.nextStepLabel || nextStepLabel}
                        </Button>
                    ) : (
                        <Button
                            loading={stepValidating}
                            key="stepper-next"
                            onClick={nextStep}
                        >
                            {(stepValid[activeStep]
                                ? activeStepItem.nextStepValidLabel
                                : activeStepItem.nextStepLabel) || nextStepLabel}
                        </Button>
                    )}
                </Group>
            }

        </EntityForm>
    )
}
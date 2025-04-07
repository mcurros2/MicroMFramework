import { KeyboardEvent } from 'react';

export const usePreventEnterSubmission = () => {
    const handleKeyDown = (event: KeyboardEvent) => {
        const target = event.target as HTMLElement;

        const isTargetedKey = ['Enter', 'Return', 'NumpadEnter'].includes(event.key);

        // Check for elements that either add new lines or do not submit the form by default on "Enter"
        const nonSubmittingElements = ['TEXTAREA', 'SELECT', 'BUTTON', 'A'];

        // For input, we want to prevent default behavior for types other than 'submit'
        const isInputNotSubmit = target.tagName === 'INPUT' && (event.target as HTMLInputElement).type !== 'submit';

        if (isTargetedKey && !event.shiftKey && !event.altKey && !event.ctrlKey &&
            (nonSubmittingElements.includes(target.tagName) === false || isInputNotSubmit)) {
            event.preventDefault();
        }
    };

    return handleKeyDown;
}
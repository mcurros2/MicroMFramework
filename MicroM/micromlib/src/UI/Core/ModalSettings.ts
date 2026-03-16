import { ModalProps } from "@mantine/core";

export type ModalSettings = Partial<Omit<ModalProps, "opened">> & {
    modalId?: string;
};

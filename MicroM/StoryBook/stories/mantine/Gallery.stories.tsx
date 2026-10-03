import type { Meta, StoryObj } from "@storybook/react-vite";
import {
    ButtonsShowcase, FeedbackShowcase, InputsShowcase, MantineShowcase, NavigationShowcase, PaletteShowcase, TableShowcase, TypographyShowcase
} from "../showcase/MantineShowcase";

const meta = {
    title: "Mantine/Gallery",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const All: Story = { render: () => <MantineShowcase /> };
export const Buttons: Story = { render: () => <ButtonsShowcase /> };
export const Inputs: Story = { render: () => <InputsShowcase /> };
export const Feedback: Story = { render: () => <FeedbackShowcase /> };
export const Navigation: Story = { name: "Navigation and containers", render: () => <NavigationShowcase /> };
export const Table: Story = { render: () => <TableShowcase /> };
export const Typography: Story = { render: () => <TypographyShowcase /> };
export const Palette: Story = { render: () => <PaletteShowcase /> };

import {
    Accordion, ActionIcon, Autocomplete, Badge, Box, Button, ColorInput, ColorScheme, ColorSwatch, Divider, Group, MantineProvider,
    MantineThemeOverride, Modal, NumberInput, Paper, ScrollArea, SegmentedControl, Select, SimpleGrid, Slider, Stack, Switch, Tabs, Text,
    Textarea, TextInput, Title, Tooltip, useMantineTheme, DEFAULT_THEME, Alert
} from "@mantine/core";
import { useClipboard } from "@mantine/hooks";
import { Prism } from "@mantine/prism";
import {
    IconAlertCircle, IconCheck, IconCode, IconCopy, IconDownload, IconMoon, IconPalette, IconPlus, IconRefresh, IconSun, IconTrash, IconUpload
} from "@tabler/icons-react";
import { ReactNode, useMemo, useState } from "react";
import { MicroMProviders, StorybookLocale } from "../MicroMProviders";
import { MantineShowcase, PaletteShowcase } from "../showcase/MantineShowcase";
import { MicroMShowcase } from "../showcase/MicroMShowcase";
import { themes } from "../themes";
import { generatePalette, isValidHex, readableTextColor } from "./colorUtils";
import { setEditorTheme, useEditorTheme } from "./themeStore";
import { deepClone, downloadText, effective, getPath, parseThemeText, Path, prettyJson, setPath, themeToTypeScript } from "./themeUtils";

type Update = (path: Path, value: unknown) => void;

interface SectionProps {
    theme: MantineThemeOverride,
    update: Update,
}

const SIZES = ["xs", "sm", "md", "lg", "xl"] as const;
const SHADES = Array.from({ length: 10 }, (_, i) => ({ value: String(i), label: String(i) }));
const FONT_PRESETS = [
    "-apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif, Apple Color Emoji, Segoe UI Emoji",
    "Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif",
    "Roboto, Helvetica, Arial, sans-serif",
    "Open Sans, Arial, sans-serif",
    "Lato, Arial, sans-serif",
    "Montserrat, Arial, sans-serif",
    "Georgia, Cambria, Times New Roman, serif",
];
const MONO_PRESETS = [
    "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, Liberation Mono, Courier New, monospace",
    "Cascadia Code, Consolas, monospace",
    "Fira Code, Consolas, monospace",
];
const BUTTON_VARIANTS = ["filled", "light", "outline", "subtle", "default", "white", "gradient"];
const ACTION_ICON_VARIANTS = ["filled", "light", "outline", "subtle", "default", "transparent"];

function useColorNames(theme: MantineThemeOverride) {
    return useMemo(() => {
        const names = new Set<string>(Object.keys(DEFAULT_THEME.colors));
        Object.keys(theme.colors ?? {}).forEach(n => names.add(n));
        return [...names];
    }, [theme.colors]);
}

function PaletteEditor({ name, shades, isOverride, onChange, onDelete }: {
    name: string, shades: string[], isOverride: boolean, onChange: (s: string[]) => void, onDelete: () => void
}) {
    const [selected, setSelected] = useState(6);
    const [base, setBase] = useState(shades[6] ?? "#228be6");
    const [baseIndex, setBaseIndex] = useState("6");

    return (
        <Paper withBorder p="xs">
            <Stack spacing="xs">
                <Group position="apart">
                    <Group spacing={6}>
                        <Text fw={600} size="sm">{name}</Text>
                        {isOverride && <Badge size="xs" variant="outline">overrides default</Badge>}
                    </Group>
                    <Tooltip label="Delete palette"><ActionIcon aria-label={`Delete palette ${name}`} color="red" variant="subtle" onClick={onDelete}><IconTrash size="1rem" /></ActionIcon></Tooltip>
                </Group>
                <Group spacing={3} noWrap>
                    {shades.map((c, i) => (
                        <ColorSwatch key={i} color={c} size={26} radius="sm" component="button" type="button" aria-label={`${name} shade ${i}`} onClick={() => setSelected(i)}
                            sx={{ cursor: "pointer", outline: i === selected ? "2px solid currentColor" : undefined, outlineOffset: 1 }}>
                            <Text size={9} color={readableTextColor(c)}>{i}</Text>
                        </ColorSwatch>
                    ))}
                </Group>
                <ColorInput size="xs" label={`Shade ${selected}`} value={shades[selected]} format="hex" withEyeDropper={false}
                    onChange={v => { if (isValidHex(v)) { const s = [...shades]; s[selected] = v; onChange(s); } }} />
                <Group spacing="xs" align="flex-end" noWrap>
                    <ColorInput size="xs" label="Generate from base color" value={base} onChange={setBase} format="hex" withEyeDropper={false} sx={{ flex: 1 }} />
                    <Select size="xs" label="At shade" data={SHADES} value={baseIndex} onChange={v => setBaseIndex(v ?? "6")} w={70} />
                    <Tooltip label="Regenerate the 10 shades">
                        <ActionIcon aria-label={`Regenerate palette ${name}`} size="lg" variant="light" disabled={!isValidHex(base)} onClick={() => onChange(generatePalette(base, parseInt(baseIndex, 10)))}>
                            <IconRefresh size="1rem" />
                        </ActionIcon>
                    </Tooltip>
                </Group>
            </Stack>
        </Paper>
    );
}

function ColorsSection({ theme, update }: SectionProps) {
    const colorNames = useColorNames(theme);
    const custom = (theme.colors ?? {}) as Record<string, string[]>;
    const [newName, setNewName] = useState("");
    const [newBase, setNewBase] = useState("#1f86a8");

    const primaryShade = theme.primaryShade;
    const shadeLight = typeof primaryShade === "number" ? primaryShade : (primaryShade?.light ?? 6);
    const shadeDark = typeof primaryShade === "number" ? primaryShade : (primaryShade?.dark ?? 8);

    const addPalette = () => {
        const name = newName.trim();
        if (!name) return;
        const existingDefault = (DEFAULT_THEME.colors as Record<string, readonly string[]>)[name];
        const shades = isValidHex(newBase) && !existingDefault ? generatePalette(newBase) : [...(existingDefault ?? generatePalette(newBase))];
        update(["colors", name], shades);
        setNewName("");
    };

    return (
        <Stack spacing="sm">
            <Select label="primaryColor" description="Default palette for buttons, inputs, links, etc." searchable
                data={colorNames} value={(theme.primaryColor as string) ?? DEFAULT_THEME.primaryColor}
                onChange={v => update(["primaryColor"], v === DEFAULT_THEME.primaryColor ? undefined : v)} />
            <SimpleGrid cols={2}>
                <Select label="primaryShade light" data={SHADES} value={String(shadeLight)}
                    onChange={v => update(["primaryShade"], { light: parseInt(v ?? "6", 10), dark: shadeDark })} />
                <Select label="primaryShade dark" data={SHADES} value={String(shadeDark)}
                    onChange={v => update(["primaryShade"], { light: shadeLight, dark: parseInt(v ?? "8", 10) })} />
            </SimpleGrid>
            <SimpleGrid cols={2}>
                <ColorInput label="white" placeholder={DEFAULT_THEME.white} value={theme.white ?? ""} format="hex" withEyeDropper={false}
                    onChange={v => update(["white"], v || undefined)} />
                <ColorInput label="black" placeholder={DEFAULT_THEME.black} value={theme.black ?? ""} format="hex" withEyeDropper={false}
                    onChange={v => update(["black"], v || undefined)} />
            </SimpleGrid>

            <Divider label="Custom palettes" labelPosition="center" />
            <Text size="xs" color="dimmed">
                Add a new palette (e.g. "brand") generated from a color, or type the name of a Mantine palette
                (blue, gray, dark...) to override it.
            </Text>
            <Group spacing="xs" align="flex-end" noWrap>
                <Autocomplete size="xs" label="Name" placeholder="brand" data={Object.keys(DEFAULT_THEME.colors)} value={newName} onChange={setNewName} sx={{ flex: 1 }} />
                <ColorInput size="xs" label="Base color" value={newBase} onChange={setNewBase} format="hex" withEyeDropper={false} w={130} />
                <Tooltip label="Add palette">
                    <ActionIcon aria-label="Add palette" size="lg" variant="filled" color="blue" disabled={!newName.trim()} onClick={addPalette}><IconPlus size="1rem" /></ActionIcon>
                </Tooltip>
            </Group>
            {Object.entries(custom).map(([name, shades]) => (
                <PaletteEditor key={name} name={name} shades={shades}
                    isOverride={name in DEFAULT_THEME.colors}
                    onChange={s => update(["colors", name], s)}
                    onDelete={() => {
                        update(["colors", name], undefined);
                        if (theme.primaryColor === name) update(["primaryColor"], undefined);
                    }} />
            ))}
        </Stack>
    );
}

function SizesInputs({ theme, update, path, label }: SectionProps & { path: string, label: string }) {
    return (
        <Stack spacing={4}>
            <Text size="sm" fw={500}>{label}</Text>
            <SimpleGrid cols={5} spacing={6}>
                {SIZES.map(s => (
                    <TextInput key={s} size="xs" label={s} placeholder={String(getPath(DEFAULT_THEME, [path, s]))}
                        value={(getPath(theme, [path, s]) as string) ?? ""} onChange={e => update([path, s], e.currentTarget.value || undefined)} />
                ))}
            </SimpleGrid>
        </Stack>
    );
}

function TypographySection({ theme, update }: SectionProps) {
    return (
        <Stack spacing="sm">
            <Autocomplete label="fontFamily" description="The font must be available (installed or loaded by the app)"
                placeholder="Mantine default (system-ui)" data={FONT_PRESETS} value={(theme.fontFamily as string) ?? ""}
                onChange={v => update(["fontFamily"], v || undefined)} />
            <Autocomplete label="headings.fontFamily" placeholder="Same as fontFamily" data={FONT_PRESETS}
                value={(getPath(theme, ["headings", "fontFamily"]) as string) ?? ""} onChange={v => update(["headings", "fontFamily"], v || undefined)} />
            <Autocomplete label="fontFamilyMonospace" placeholder="Mantine default" data={MONO_PRESETS}
                value={(theme.fontFamilyMonospace as string) ?? ""} onChange={v => update(["fontFamilyMonospace"], v || undefined)} />
            <SimpleGrid cols={2}>
                <Select label="headings.fontWeight" data={["400", "500", "600", "700", "800", "900"]}
                    value={String(effective(theme, ["headings", "fontWeight"]))}
                    onChange={v => update(["headings", "fontWeight"], v ? parseInt(v, 10) : undefined)} />
                <NumberInput label="lineHeight" precision={2} step={0.05} min={1} max={2.5}
                    placeholder={String(DEFAULT_THEME.lineHeight)} value={(theme.lineHeight as number) ?? ""}
                    onChange={v => update(["lineHeight"], v === "" ? undefined : v)} />
            </SimpleGrid>
            <SizesInputs theme={theme} update={update} path="fontSizes" label="fontSizes" />
            <Stack spacing={4}>
                <Text size="sm" fw={500}>Heading sizes</Text>
                <SimpleGrid cols={3} spacing={6}>
                    {(["h1", "h2", "h3", "h4", "h5", "h6"] as const).map(h => (
                        <TextInput key={h} size="xs" label={h} placeholder={String(getPath(DEFAULT_THEME, ["headings", "sizes", h, "fontSize"]))}
                            value={(getPath(theme, ["headings", "sizes", h, "fontSize"]) as string) ?? ""}
                            onChange={e => update(["headings", "sizes", h, "fontSize"], e.currentTarget.value || undefined)} />
                    ))}
                </SimpleGrid>
            </Stack>
        </Stack>
    );
}

function ShapeSection({ theme, update }: SectionProps) {
    return (
        <Stack spacing="sm">
            <Stack spacing={4}>
                <Text size="sm" fw={500}>defaultRadius</Text>
                <SegmentedControl size="xs" fullWidth data={["0", ...SIZES]} value={String(effective(theme, ["defaultRadius"]))}
                    onChange={v => update(["defaultRadius"], v === DEFAULT_THEME.defaultRadius ? undefined : (v === "0" ? 0 : v))} />
            </Stack>
            <SizesInputs theme={theme} update={update} path="radius" label="radius" />
            <SizesInputs theme={theme} update={update} path="spacing" label="spacing" />
            <Stack spacing={4}>
                <Text size="sm" fw={500}>shadows</Text>
                {SIZES.map(s => (
                    <TextInput key={s} size="xs" label={s} placeholder={DEFAULT_THEME.shadows[s]}
                        value={(getPath(theme, ["shadows", s]) as string) ?? ""} onChange={e => update(["shadows", s], e.currentTarget.value || undefined)} />
                ))}
            </Stack>
        </Stack>
    );
}

function OthersSection({ theme, update }: SectionProps) {
    const colorNames = useColorNames(theme);
    const grad = { ...DEFAULT_THEME.defaultGradient, ...(theme.defaultGradient ?? {}) };
    return (
        <Stack spacing="sm">
            <SimpleGrid cols={2}>
                <Select label="focusRing" data={["auto", "always", "never"]} value={effective(theme, ["focusRing"])}
                    onChange={v => update(["focusRing"], v === DEFAULT_THEME.focusRing ? undefined : v)} />
                <Select label="cursorType" data={["default", "pointer"]} value={effective(theme, ["cursorType"])}
                    onChange={v => update(["cursorType"], v === DEFAULT_THEME.cursorType ? undefined : v)} />
                <Select label="loader" data={["oval", "bars", "dots"]} value={effective(theme, ["loader"])}
                    onChange={v => update(["loader"], v === DEFAULT_THEME.loader ? undefined : v)} />
                <Switch mt="xl" label="respectReducedMotion" checked={effective<boolean>(theme, ["respectReducedMotion"])}
                    onChange={e => update(["respectReducedMotion"], e.currentTarget.checked === DEFAULT_THEME.respectReducedMotion ? undefined : e.currentTarget.checked)} />
            </SimpleGrid>
            <Text size="sm" fw={500}>defaultGradient</Text>
            <SimpleGrid cols={2}>
                <Select size="xs" label="from" data={colorNames} searchable value={grad.from} onChange={v => update(["defaultGradient"], { ...grad, from: v })} />
                <Select size="xs" label="to" data={colorNames} searchable value={grad.to} onChange={v => update(["defaultGradient"], { ...grad, to: v })} />
            </SimpleGrid>
            <Slider label={v => `${v}°`} min={0} max={360} value={grad.deg ?? 45} onChange={v => update(["defaultGradient"], { ...grad, deg: v })} />
        </Stack>
    );
}

function componentSelect(theme: MantineThemeOverride, update: Update, component: string, prop: string, data: string[], def: string) {
    const path = ["components", component, "defaultProps", prop];
    return (
        <Select size="xs" label={`${component}.${prop}`} data={data} placeholder={`${def} (default)`} clearable
            value={(getPath(theme, path) as string) ?? null} onChange={v => update(path, v ?? undefined)} />
    );
}

function componentSwitch(theme: MantineThemeOverride, update: Update, component: string, prop: string, def: boolean) {
    const path = ["components", component, "defaultProps", prop];
    const v = getPath(theme, path) as boolean | undefined;
    return (
        <Switch size="xs" label={`${component}.${prop}`} checked={v ?? def}
            onChange={e => update(path, e.currentTarget.checked === def ? undefined : e.currentTarget.checked)} />
    );
}

function ComponentsSection({ theme, update }: SectionProps) {
    const colorNames = useColorNames(theme);
    const [json, setJson] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const current = JSON.stringify(theme.components ?? {}, null, 2);

    return (
        <Stack spacing="sm">
            <Text size="xs" color="dimmed">
                MicroM components use <code>useComponentDefaultProps</code>, so their defaults can be set through
                <code> theme.components</code> just like Mantine's.
            </Text>
            <SimpleGrid cols={2} spacing="xs">
                {componentSelect(theme, update, "EntityForm", "okButtonVariant", BUTTON_VARIANTS, "filled")}
                {componentSelect(theme, update, "EntityForm", "cancelButtonVariant", BUTTON_VARIANTS, "light")}
                {componentSelect(theme, update, "EntityForm", "isDirtyColor", colorNames, "green")}
                {componentSelect(theme, update, "DataGrid", "toolbarIconVariant", ACTION_ICON_VARIANTS, "light")}
                {componentSelect(theme, update, "DataGrid", "actionsButtonVariant", BUTTON_VARIANTS, "light")}
                {componentSelect(theme, update, "Lookup", "iconVariant", ACTION_ICON_VARIANTS, "light")}
                {componentSelect(theme, update, "LookupSelect", "editButtonVariant", BUTTON_VARIANTS, "light")}
                {componentSelect(theme, update, "Button", "variant", BUTTON_VARIANTS, "filled")}
                {componentSelect(theme, update, "TextInput", "variant", ["default", "filled", "unstyled"], "default")}
                {componentSelect(theme, update, "Card", "radius", ["0", ...SIZES], "sm")}
            </SimpleGrid>
            <Stack spacing={6}>
                {componentSwitch(theme, update, "DataGrid", "columnBorders", true)}
                {componentSwitch(theme, update, "DataGrid", "rowBorders", false)}
                {componentSwitch(theme, update, "DataGrid", "withBorder", true)}
                {componentSwitch(theme, update, "Card", "withBorder", false)}
            </Stack>
            <Divider label="theme.components (advanced JSON)" labelPosition="center" />
            <Textarea autosize minRows={4} maxRows={16} styles={{ input: { fontFamily: "monospace", fontSize: 12 } }}
                value={json ?? current} error={error}
                onChange={e => { setJson(e.currentTarget.value); setError(null); }} />
            <Group position="right" spacing="xs">
                <Button size="xs" variant="default" disabled={json === null} onClick={() => { setJson(null); setError(null); }}>Discard</Button>
                <Button size="xs" disabled={json === null} onClick={() => {
                    try {
                        const parsed = JSON.parse(json ?? "{}");
                        update(["components"], Object.keys(parsed).length ? parsed : undefined);
                        setJson(null);
                    } catch (e) {
                        setError(`Invalid JSON: ${(e as Error).message}`);
                    }
                }}>Apply JSON</Button>
            </Group>
        </Stack>
    );
}

function ExportBar({ theme, onImport }: { theme: MantineThemeOverride, onImport: (t: MantineThemeOverride) => void }) {
    const [exportName, setExportName] = useState("myTheme");
    const [showCode, setShowCode] = useState(false);
    const [showImport, setShowImport] = useState(false);
    const [importText, setImportText] = useState("");
    const [importError, setImportError] = useState<string | null>(null);
    const clipboard = useClipboard({ timeout: 1500 });
    const code = useMemo(() => themeToTypeScript(theme, exportName), [theme, exportName]);

    return (
        <Stack spacing="xs">
            <TextInput size="xs" label="Exported constant name" value={exportName} onChange={e => setExportName(e.currentTarget.value)} />
            <Group spacing="xs" grow>
                <Button size="xs" leftIcon={clipboard.copied ? <IconCheck size="0.9rem" /> : <IconCopy size="0.9rem" />} color={clipboard.copied ? "teal" : "blue"}
                    onClick={() => clipboard.copy(code)}>{clipboard.copied ? "Copied" : "Copy .ts"}</Button>
                <Button size="xs" variant="light" leftIcon={<IconCode size="0.9rem" />} onClick={() => setShowCode(true)}>View code</Button>
            </Group>
            <Group spacing="xs" grow>
                <Button size="xs" variant="default" leftIcon={<IconDownload size="0.9rem" />} onClick={() => downloadText(`${exportName || "theme"}.ts`, code, "text/typescript")}>.ts</Button>
                <Button size="xs" variant="default" leftIcon={<IconDownload size="0.9rem" />} onClick={() => downloadText(`${exportName || "theme"}.json`, prettyJson(theme), "application/json")}>.json</Button>
                <Button size="xs" variant="default" leftIcon={<IconUpload size="0.9rem" />} onClick={() => { setImportText(""); setImportError(null); setShowImport(true); }}>Import</Button>
            </Group>

            <Modal opened={showCode} onClose={() => setShowCode(false)} title="Exported theme" size="xl">
                <Prism language="tsx" copyLabel="Copy" copiedLabel="Copied">{code}</Prism>
            </Modal>
            <Modal opened={showImport} onClose={() => setShowImport(false)} title="Import theme" size="lg">
                <Stack>
                    <Text size="sm">Paste a theme JSON or the content of a .ts exported by this editor.</Text>
                    <Textarea autosize minRows={8} maxRows={20} styles={{ input: { fontFamily: "monospace", fontSize: 12 } }}
                        value={importText} onChange={e => { setImportText(e.currentTarget.value); setImportError(null); }} error={importError} />
                    <Group position="right">
                        <Button variant="default" onClick={() => setShowImport(false)}>Cancel</Button>
                        <Button onClick={() => {
                            try {
                                onImport(parseThemeText(importText));
                                setShowImport(false);
                            } catch (e) {
                                setImportError((e as Error).message);
                            }
                        }}>Import</Button>
                    </Group>
                </Stack>
            </Modal>
        </Stack>
    );
}

function PreviewSurface({ children }: { children: ReactNode }) {
    const theme = useMantineTheme();
    return (
        <Box sx={{
            minHeight: "100%",
            padding: theme.spacing.md,
            backgroundColor: theme.colorScheme === "dark" ? theme.colors.dark[7] : theme.white,
            color: theme.colorScheme === "dark" ? theme.colors.dark[0] : theme.black,
            fontFamily: theme.fontFamily,
        }}>
            {children}
        </Box>
    );
}

export interface ThemeEditorProps {
    initialColorScheme?: ColorScheme,
    locale?: StorybookLocale,
}

export function ThemeEditor({ initialColorScheme = "light", locale = "en-US" }: ThemeEditorProps) {
    const theme = useEditorTheme();
    const [colorScheme, setColorScheme] = useState<ColorScheme>(initialColorScheme);
    const [baseId, setBaseId] = useState<string | null>(null);
    const [previewTab, setPreviewTab] = useState<string | null>("microm");

    const update: Update = (path, value) => setEditorTheme(setPath(theme, path, value));
    const overridesCount = Object.keys(theme).length;

    return (
        <Box sx={{ display: "flex", height: "100vh", overflow: "hidden" }}>
            {/* Controls always use the default Mantine theme so editing never breaks them */}
            <MantineProvider theme={{ colorScheme }} inherit={false}>
                <Paper radius={0} withBorder sx={{ width: 400, minWidth: 340, display: "flex", flexDirection: "column", height: "100vh" }}>
                    <Stack spacing="xs" p="sm">
                        <Group position="apart">
                            <Group spacing={6}><IconPalette size="1.3rem" /><Title order={4}>Theme Editor</Title></Group>
                            <Group spacing={4}>
                                <Badge variant="light">{overridesCount} keys</Badge>
                                <Tooltip label={colorScheme === "dark" ? "Light mode" : "Dark mode"}>
                                    <ActionIcon aria-label="Toggle color scheme" variant="default" onClick={() => setColorScheme(c => (c === "dark" ? "light" : "dark"))}>
                                        {colorScheme === "dark" ? <IconSun size="1rem" /> : <IconMoon size="1rem" />}
                                    </ActionIcon>
                                </Tooltip>
                            </Group>
                        </Group>
                        <Group spacing="xs" align="flex-end" noWrap>
                            <Select size="xs" label="Start from a registered theme" placeholder="Pick a theme" sx={{ flex: 1 }}
                                data={themes.map(t => ({ value: t.id, label: t.name }))} value={baseId} onChange={setBaseId} />
                            <Button size="xs" variant="light" disabled={!baseId}
                                onClick={() => { const t = themes.find(x => x.id === baseId); if (t) setEditorTheme(deepClone(t.theme)); }}>Load</Button>
                            <Tooltip label="Reset to Mantine default">
                                <ActionIcon aria-label="Reset theme" size="lg" variant="default" onClick={() => setEditorTheme({})}><IconRefresh size="1rem" /></ActionIcon>
                            </Tooltip>
                        </Group>
                    </Stack>
                    <ScrollArea sx={{ flex: 1 }} px="sm">
                        <Accordion multiple defaultValue={["colores"]} variant="contained" mb="sm">
                            <Accordion.Item value="colores">
                                <Accordion.Control>Colors</Accordion.Control>
                                <Accordion.Panel><ColorsSection theme={theme} update={update} /></Accordion.Panel>
                            </Accordion.Item>
                            <Accordion.Item value="tipografia">
                                <Accordion.Control>Typography</Accordion.Control>
                                <Accordion.Panel><TypographySection theme={theme} update={update} /></Accordion.Panel>
                            </Accordion.Item>
                            <Accordion.Item value="forma">
                                <Accordion.Control>Shape, spacing and shadows</Accordion.Control>
                                <Accordion.Panel><ShapeSection theme={theme} update={update} /></Accordion.Panel>
                            </Accordion.Item>
                            <Accordion.Item value="otros">
                                <Accordion.Control>Other</Accordion.Control>
                                <Accordion.Panel><OthersSection theme={theme} update={update} /></Accordion.Panel>
                            </Accordion.Item>
                            <Accordion.Item value="componentes">
                                <Accordion.Control>Component defaults (MicroM)</Accordion.Control>
                                <Accordion.Panel><ComponentsSection theme={theme} update={update} /></Accordion.Panel>
                            </Accordion.Item>
                        </Accordion>
                    </ScrollArea>
                    <Box p="sm" sx={t => ({ borderTop: `1px solid ${t.colorScheme === "dark" ? t.colors.dark[4] : t.colors.gray[3]}` })}>
                        <ExportBar theme={theme} onImport={setEditorTheme} />
                    </Box>
                </Paper>
            </MantineProvider>

            <Box sx={{ flex: 1, overflow: "auto" }}>
                <MicroMProviders theme={theme} colorScheme={colorScheme} locale={locale} toggleColorScheme={v => setColorScheme(v ?? (colorScheme === "dark" ? "light" : "dark"))}>
                    <PreviewSurface>
                        <Stack>
                            <Alert icon={<IconAlertCircle size="1rem" />} variant="light" title="Editor theme">
                                Changes are saved in this browser. To apply it to the other stories pick
                                "Theme Editor" in the toolbar. When it is ready, export it as .ts and register it in <code>stories/themes</code> or in your app.
                            </Alert>
                            <Tabs value={previewTab} onTabChange={setPreviewTab} keepMounted={false}>
                                <Tabs.List>
                                    <Tabs.Tab value="microm">MicroM</Tabs.Tab>
                                    <Tabs.Tab value="mantine">Mantine</Tabs.Tab>
                                    <Tabs.Tab value="palette">Palette</Tabs.Tab>
                                </Tabs.List>
                                <Tabs.Panel value="microm" pt="md"><MicroMShowcase /></Tabs.Panel>
                                <Tabs.Panel value="mantine" pt="md"><MantineShowcase /></Tabs.Panel>
                                <Tabs.Panel value="palette" pt="md"><PaletteShowcase /></Tabs.Panel>
                            </Tabs>
                        </Stack>
                    </PreviewSurface>
                </MicroMProviders>
            </Box>
        </Box>
    );
}

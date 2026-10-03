import type { StorybookConfig } from "@storybook/react-vite";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { mergeConfig } from "vite";

const dirname = path.dirname(fileURLToPath(import.meta.url));
const micromlibRoot = path.resolve(dirname, "../../micromlib");
const micromlibSrc = path.join(micromlibRoot, "src");

// Must resolve from StoryBook/node_modules only; otherwise micromlib/src picks up micromlib/node_modules
// and we end up with two React/Mantine instances (separate contexts and themes).
const shared = [
    "react", "react-dom", "react/jsx-runtime", "react/jsx-dev-runtime",
    "@mantine/core", "@mantine/hooks", "@mantine/dates", "@mantine/form", "@mantine/modals",
    "@mantine/prism", "@mantine/spotlight", "@mantine/dropzone", "@mantine/styles", "@mantine/utils",
    "@emotion/react", "dayjs", "mime", "exceljs", "react-easy-crop", "xls-reader",
    "@googlemaps/js-api-loader", "@googlemaps/markerclusterer",
];

const config: StorybookConfig = {
    stories: ["../stories/**/*.mdx", "../stories/**/*.stories.@(ts|tsx)"],
    addons: ["@storybook/addon-docs"],
    framework: {
        name: "@storybook/react-vite",
        options: {},
    },
    core: {
        disableTelemetry: true,
    },
    typescript: {
        // Docgen would parse every micromlib source file on each request
        reactDocgen: false,
    },
    async viteFinal(viteConfig) {
        return mergeConfig(viteConfig, {
            resolve: {
                alias: [
                    { find: /^@mcurros2\/microm$/, replacement: path.join(micromlibSrc, "index.ts") },
                    // micromlib/src/UI/Grid/Grid.tsx imports "client" through micromlib's tsconfig baseUrl
                    { find: /^client$/, replacement: path.join(micromlibSrc, "client") },
                    // The package entry re-exports dynamic imports, which makes the optimizer emit one chunk per icon (~6000 requests)
                    { find: /^@tabler\/icons-react$/, replacement: "@tabler/icons-react/dist/esm/icons/index.mjs" },
                ],
                dedupe: shared,
            },
            server: {
                fs: {
                    allow: [path.resolve(dirname, ".."), micromlibRoot],
                },
            },
            optimizeDeps: {
                include: [
                    "react", "react-dom", "react-dom/client", "react/jsx-runtime", "react/jsx-dev-runtime",
                    "@mantine/core", "@mantine/hooks", "@mantine/dates", "@mantine/form", "@mantine/modals",
                    "@mantine/prism", "@mantine/dropzone", "@mantine/spotlight", "@emotion/react",
                    "@tabler/icons-react/dist/esm/icons/index.mjs",
                    "dayjs", "dayjs/locale/es", "dayjs/plugin/customParseFormat",
                    "exceljs", "xls-reader", "mime", "react-easy-crop",
                    "@googlemaps/js-api-loader", "@googlemaps/markerclusterer",
                ],
            },
        });
    },
};

export default config;

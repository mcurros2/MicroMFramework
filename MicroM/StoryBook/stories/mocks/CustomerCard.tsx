import { Badge, Group, Stack, Text } from "@mantine/core";
import { EntityCardProps, ValuesObject } from "@mcurros2/microm";
import { IconMail, IconPhone } from "@tabler/icons-react";

// record.data keys are the camelCased view headers ("Full name" -> fullName)
export function CustomerCard({ record }: EntityCardProps<ValuesObject>) {
    const d = record.data;
    return (
        <Stack spacing={4}>
            <Group position="apart" noWrap>
                <Text fw={600} lineClamp={1}>{String(d.fullName ?? "")}</Text>
                <Badge variant="light">{String(d.category ?? "")}</Badge>
            </Group>
            <Group spacing={4} noWrap><IconMail size="0.9rem" /><Text size="sm" color="dimmed" lineClamp={1}>{String(d.email ?? "")}</Text></Group>
            <Group spacing={4} noWrap><IconPhone size="0.9rem" /><Text size="sm" color="dimmed">{String(d.phone ?? "")}</Text></Group>
            <Group position="apart">
                <Text size="xs" color="dimmed">{String(d.country ?? "")} - since {String(d.customerSince ?? "")}</Text>
                <Text size="sm" fw={500}>{String(d.creditLimit ?? "")}</Text>
            </Group>
        </Stack>
    );
}

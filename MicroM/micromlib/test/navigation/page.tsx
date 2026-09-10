import { MantineProvider, Button, Stack, TextInput, Select, Text } from '@mantine/core';
import { createRoot } from 'react-dom/client';
import { useState } from 'react';
import { ModalsManager, useModal } from '../../src/UI/Core/ModalsManager';
import { useConfirmNavigation } from '../../src/UI/Router/useConfirmNavigation';
import { NavigationProtectionMode } from '../../src/UI/Router/NavigationGuards';
import { MicroMRouter } from '../../src/UI/Router/MicroMRouter';
import { useMicroMRouter } from '../../src/UI/Router/useMicroMRouter';

function TestForm({ depth }: { depth: number }) {
    const modal = useModal();
    const [value, setValue] = useState('');
    const [mode, setMode] = useState<NavigationProtectionMode>('confirm');
    const [fail, setFail] = useState(false);
    const [view, setView] = useState(false);
    const [calls, setCalls] = useState(0);
    useConfirmNavigation({
        mode,
        hasUnsavedChanges: () => !view && (mode === 'allways' || value !== ''),
        onSave: async () => { setCalls(count => count + 1); if (fail) return false; await modal.close(); return true; },
        onLeave: async () => { setCalls(count => count + 1); if (fail) return false; await modal.close(); return true; },
    });
    return <Stack>
        <Text>Form {depth}</Text>
        <Text>Calls: {calls}</Text>
        <TextInput label={`Value ${depth}`} value={value} onChange={event => setValue(event.currentTarget.value)} />
        <Select label={`Protection ${depth}`} value={mode} data={['confirm', 'save', 'allways', 'disabled']} onChange={value => setMode(value as NavigationProtectionMode)} />
        <Button onClick={() => setFail(!fail)}>Failure: {String(fail)}</Button>
        <Button onClick={() => setView(!view)}>View: {String(view)}</Button>
        <Button onClick={() => void modal.open({ content: <TestForm depth={depth + 1} />, modalProps: { title: `Modal ${depth + 1}`, closeOnClickOutside: true } })}>Open nested</Button>
        <Button onClick={() => void modal.close()}>Explicit close</Button>
    </Stack>;
}

function Page() {
    const modal = useModal();
    const { route, navigate } = useMicroMRouter();
    return <Stack>
        <Text>Route: {route}</Text>
        <Button onClick={() => navigate('/orders')}>Orders route</Button>
        <Button onClick={() => navigate('/other')}>Other route</Button>
        <Button onClick={() => void modal.open({ content: <TestForm depth={1} />, modalProps: { title: 'Modal 1', closeOnClickOutside: true } })}>Open form</Button>
        <Button onClick={() => void modal.open({ content: Promise.resolve(<TestForm depth={1} />), modalProps: { title: 'Async form', closeOnClickOutside: true } })}>Open async</Button>
    </Stack>;
}

createRoot(document.getElementById('root')!).render(<MantineProvider withGlobalStyles withNormalizeCSS>
    <ModalsManager modalProps={{}} animationDuration={0}><MicroMRouter><Page /></MicroMRouter></ModalsManager>
</MantineProvider>);

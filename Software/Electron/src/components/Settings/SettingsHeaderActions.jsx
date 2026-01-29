import React from 'react';
import { ActionIcon } from '@mantine/core';
import { IconDownload, IconDeviceFloppy } from '@tabler/icons-react';

export default function SettingsHeaderActions({ onLoad, onSave }) {
    return (
        <>
            <ActionIcon
                size="sm"
                variant="subtle"
                color="gray"
                title="Зчитати"
                onClick={onLoad}
            >
                <IconDownload size={16} />
            </ActionIcon>
            <ActionIcon
                size="sm"
                variant="subtle"
                color="gray"
                title="Зберегти"
                onClick={onSave}
            >
                <IconDeviceFloppy size={16} />
            </ActionIcon>
        </>
    );
}

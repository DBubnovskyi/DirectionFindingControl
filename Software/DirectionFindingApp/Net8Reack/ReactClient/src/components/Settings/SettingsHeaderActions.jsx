import React from 'react';
import { ActionIcon } from '@mantine/core';
import { IconDownload, IconDeviceFloppy } from '@tabler/icons-react';
import { useApp } from '../../contexts/AppContext';

export default function SettingsHeaderActions() {
    const { refreshServerState, saveSettingsToServer } = useApp();

    const handleLoad = () => {
        refreshServerState();
    };

    const handleSave = () => {
        const savedSettings = localStorage.getItem('rotator-settings');
        if (savedSettings) {
            const settings = JSON.parse(savedSettings);
            const speedRange = settings.speedRange || [0, 100];

            const params = {
                tolerance: parseFloat(settings.error) || 1.0,
                minSpeed: speedRange[0],
                maxSpeed: speedRange[1],
                brake: parseFloat(settings.brakeAngle) || 15.0
            };
            saveSettingsToServer(params);
        }
    };

    return (
        <>
            <ActionIcon
                size="sm"
                variant="subtle"
                color="gray"
                title="Зчитати"
                onClick={handleLoad}
            >
                <IconDownload size={16} />
            </ActionIcon>
            <ActionIcon
                size="sm"
                variant="subtle"
                color="gray"
                title="Зберегти"
                onClick={handleSave}
            >
                <IconDeviceFloppy size={16} />
            </ActionIcon>
        </>
    );
}
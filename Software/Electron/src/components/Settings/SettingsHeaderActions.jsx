import React from 'react';
import { ActionIcon } from '@mantine/core';
import { IconDownload, IconDeviceFloppy } from '@tabler/icons-react';
import { useRotator } from '../../contexts/RotatorContext';

export default function SettingsHeaderActions() {
    const { sendGetCommand, sendSetCommand } = useRotator();

    const handleLoad = () => {
        console.log('🔴 SettingsHeaderActions.handleLoad clicked');
        sendGetCommand(['TOL', 'MINS', 'MAXS', 'BRK']);
        console.log('Requesting settings from device...');
    };

    const handleSave = () => {
        console.log('🔴 SettingsHeaderActions.handleSave clicked');
        // Отримуємо збережені налаштування з localStorage
        const savedSettings = localStorage.getItem('rotator-settings');
        if (savedSettings) {
            const settings = JSON.parse(savedSettings);
            console.log('📂 Loaded settings from localStorage:', settings);

            // Перевіряємо наявність speedRange та використовуємо значення за замовчуванням
            const speedRange = settings.speedRange || [0, 100];

            const params = {
                tolerance: parseFloat(settings.error) || 1.0,
                minSpeed: speedRange[0],
                maxSpeed: speedRange[1],
                brake: parseFloat(settings.brakeAngle) || 15.0
            };
            console.log('🔴 Params to send (in %):', params);
            sendSetCommand(params);
            console.log('✅ Settings saved to device');
        } else {
            console.warn('⚠️ No settings found in localStorage');
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


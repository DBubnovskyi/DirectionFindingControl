import React, { useState, useEffect } from 'react';
import { TextInput, Stack, RangeSlider, Text } from '@mantine/core';
import { useRotator } from '../../contexts/RotatorContext';
import SettingsHeaderActions from './SettingsHeaderActions';
import './Settings.css';

function Settings() {
    const { settings: rotatorSettings, sendGetCommand, sendSetCommand } = useRotator();

    const [settings, setSettings] = useState({
        error: '',
        speedRange: [0, 100],
        brakeAngle: ''
    });

    // Синхронізуємо з RotatorContext
    useEffect(() => {
        setSettings(prev => ({
            ...prev,
            error: rotatorSettings.tolerance?.toString() || '',
            speedRange: [rotatorSettings.minSpeed || 0, rotatorSettings.maxSpeed || 100],
            brakeAngle: rotatorSettings.brake?.toString() || ''
        }));
    }, [rotatorSettings]);

    useEffect(() => {
        loadSettings();
    }, []);

    const loadSettings = () => {
        const savedSettings = localStorage.getItem('rotator-settings');
        if (savedSettings) {
            const parsed = JSON.parse(savedSettings);
            // Якщо немає speedRange, створюємо з minSpeed/maxSpeed або значення за замовчуванням
            if (!parsed.speedRange && (parsed.minSpeed || parsed.maxSpeed)) {
                parsed.speedRange = [
                    parseInt(parsed.minSpeed) || 0,
                    parseInt(parsed.maxSpeed) || 100
                ];
            } else if (!parsed.speedRange) {
                parsed.speedRange = [0, 100];
            }
            setSettings(parsed);
        }
    };

    const handleChange = (field, value) => {
        const newSettings = { ...settings, [field]: value };
        setSettings(newSettings);
        // Зберігаємо одразу в localStorage для синхронізації з headerActions
        localStorage.setItem('rotator-settings', JSON.stringify(newSettings));
    };

    const handleSpeedRangeChange = (value) => {
        const newSettings = { ...settings, speedRange: value };
        setSettings(newSettings);
        // Зберігаємо одразу в localStorage для синхронізації з headerActions
        localStorage.setItem('rotator-settings', JSON.stringify(newSettings));
    };

    const handleSave = () => {
        console.log('💾 Settings.handleSave - Current settings:', settings);

        // Відправляємо налаштування на пристрій
        sendSetCommand({
            tolerance: parseFloat(settings.error) || rotatorSettings.tolerance,
            minSpeed: settings.speedRange[0],
            maxSpeed: settings.speedRange[1],
            brake: parseFloat(settings.brakeAngle) || rotatorSettings.brake
        });

        // Зберігаємо в localStorage
        localStorage.setItem('rotator-settings', JSON.stringify(settings));
        console.log('💾 Settings saved to localStorage and device');
    };

    return (
        <div className="settings-container">
            <Stack gap="md">
                <TextInput
                    label="Похибка"
                    placeholder="Введіть похибку"
                    value={settings.error}
                    onChange={(e) => handleChange('error', e.target.value)}
                />
                <div style={{ width: '95%', padding: '10px 0 20px 0' }}>
                    <Text size="sm" fw={155} mb={8}>
                        Діапазон швидкості: {settings.speedRange[0]}% - {settings.speedRange[1]}%
                    </Text>
                    <RangeSlider
                        min={0}
                        max={100}
                        step={1}
                        value={settings.speedRange}
                        onChange={handleSpeedRangeChange}
                        marks={[
                            { value: 0, label: '0%' },
                            { value: 25, label: '25%' },
                            { value: 50, label: '50%' },
                            { value: 75, label: '75%' },
                            { value: 100, label: '100%' }
                        ]}
                    />
                </div>
                <TextInput
                    label="Кут гальмування"
                    placeholder="Введіть кут гальмування"
                    value={settings.brakeAngle}
                    onChange={(e) => handleChange('brakeAngle', e.target.value)}
                />
            </Stack>
        </div>
    );
}

// Статична властивість для headerActions, яка буде використана GridBlock
Settings.headerActions = <SettingsHeaderActions />;

export default Settings;


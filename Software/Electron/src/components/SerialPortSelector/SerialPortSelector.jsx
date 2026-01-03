import React, { useState, useEffect } from 'react';
import { Select, Button, Group, Stack, Text, Badge } from '@mantine/core';
import { IconPlugConnected, IconPlugConnectedX } from '@tabler/icons-react';
import './SerialPortSelector.css';

export default function SerialPortSelector() {
    const [selectedPort, setSelectedPort] = useState('');
    const [baudRate, setBaudRate] = useState('115200');
    const [isConnected, setIsConnected] = useState(false);
    const [availablePorts, setAvailablePorts] = useState([]);

    useEffect(() => {
        // Завантажуємо список доступних портів
        const loadPorts = async () => {
            if (window.electronAPI) {
                const ports = await window.electronAPI.listSerialPorts();
                setAvailablePorts(ports);
            } else {
                // Моковані дані для браузера
                setAvailablePorts([
                    { value: 'COM1', label: 'COM1' },
                    { value: 'COM3', label: 'COM3' },
                    { value: 'COM4', label: 'COM4' },
                ]);
            }
        };

        loadPorts();
    }, []);

    const baudRates = [
        { value: '9600', label: '9600' },
        { value: '19200', label: '19200' },
        { value: '38400', label: '38400' },
        { value: '57600', label: '57600' },
        { value: '115200', label: '115200' },
    ];

    const handleConnect = () => {
        if (!selectedPort) {
            alert('Будь ласка, виберіть порт');
            return;
        }

        // Тут буде логіка підключення через IPC до main процесу
        setIsConnected(!isConnected);
    };

    return (
        <Stack gap="md">
            <Group grow>
                <Select
                    label="Серійний порт"
                    placeholder="Виберіть порт"
                    data={availablePorts}
                    value={selectedPort}
                    onChange={setSelectedPort}
                    disabled={isConnected}
                />
                <Select
                    label="Швидкість (baud rate)"
                    data={baudRates}
                    value={baudRate}
                    onChange={setBaudRate}
                    disabled={isConnected}
                />
            </Group>

            <Group justify="space-between" align="center">
                <Badge
                    color={isConnected ? 'green' : 'gray'}
                    variant="filled"
                    size="lg"
                >
                    {isConnected ? 'Підключено' : 'Відключено'}
                </Badge>

                <Button
                    onClick={handleConnect}
                    color={isConnected ? 'red' : 'blue'}
                    leftSection={
                        isConnected ?
                            <IconPlugConnectedX size={16} /> :
                            <IconPlugConnected size={16} />
                    }
                >
                    {isConnected ? 'Відключитись' : 'Підключитись'}
                </Button>
            </Group>

            {isConnected && selectedPort && (
                <Text size="sm" c="dimmed">
                    Підключено до {selectedPort} зі швидкістю {baudRate} baud
                </Text>
            )}
        </Stack>
    );
}

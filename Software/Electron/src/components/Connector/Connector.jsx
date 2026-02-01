import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Select, Button, Group, Stack, Text, Badge } from '@mantine/core';
import { IconPlugConnected, IconPlugConnectedX } from '@tabler/icons-react';
import { useRotator } from '../../contexts/RotatorContext';
import { useLogging } from '../../contexts/LoggingContext';
import { StandardProtocol } from '../../protocols/StandardProtocol';
import './Connector.css';

export default function Connector() {
    const [selectedPort, setSelectedPort] = useState('');
    const [baudRate, setBaudRate] = useState('115200');
    const [selectedProtocol, setSelectedProtocol] = useState('standard');
    const [isConnected, setIsConnected] = useState(false);
    const [availablePorts, setAvailablePorts] = useState([]);
    const [webSerialPort, setWebSerialPort] = useState(null);
    const readerRef = useRef(null);
    const writerRef = useRef(null);
    const readLoopRef = useRef(false);
    const protocolRef = useRef(null);

    const { updateRotatorData, updateSettings, registerSendCommand } = useRotator();
    const { addLog } = useLogging();

    // Ініціалізація протоколу
    useEffect(() => {
        if (selectedProtocol === 'standard') {
            protocolRef.current = new StandardProtocol((data) => {
                // Розділяємо дані на angles та settings
                const anglesData = {};
                const settingsData = {};

                if (data.currentAzimuth !== undefined) anglesData.currentAzimuth = data.currentAzimuth;
                if (data.currentAngle !== undefined) anglesData.currentAngle = data.currentAngle;
                if (data.targetAzimuth !== undefined) anglesData.targetAzimuth = data.targetAzimuth;
                if (data.targetAngle !== undefined) anglesData.targetAngle = data.targetAngle;

                if (data.speed !== undefined) settingsData.speed = data.speed;
                if (data.minSpeed !== undefined) settingsData.minSpeed = data.minSpeed;
                if (data.maxSpeed !== undefined) settingsData.maxSpeed = data.maxSpeed;
                if (data.tolerance !== undefined) settingsData.tolerance = data.tolerance;
                if (data.brake !== undefined) settingsData.brake = data.brake;
                if (data.initMode !== undefined) settingsData.initMode = data.initMode;

                if (Object.keys(anglesData).length > 0) updateRotatorData(anglesData);
                if (Object.keys(settingsData).length > 0) updateSettings(settingsData);
            });
        }
        console.log(`🔧 Protocol initialized: ${protocolRef.current?.getName()}`);
    }, [selectedProtocol, updateRotatorData, updateSettings]);

    // Слухач для отримання даних через Electron API
    useEffect(() => {
        if (window.electronAPI && window.electronAPI.onSerialData) {
            const handleData = (data) => {
                console.log('📥 Serial received (Electron):', data);
                addLog('RX', data.trim());

                // Парсимо через протокол
                if (protocolRef.current) {
                    protocolRef.current.parseMessage(data);
                }
            };

            window.electronAPI.onSerialData(handleData);

            return () => {
                if (window.electronAPI.removeSerialDataListener) {
                    window.electronAPI.removeSerialDataListener();
                }
            };
        }
    }, [addLog]);

    // Функція для читання даних з порту
    const startReading = async (port) => {
        if (!port || !port.readable) return;

        readLoopRef.current = true;
        const reader = port.readable.getReader();
        readerRef.current = reader;

        try {
            while (readLoopRef.current) {
                const { value, done } = await reader.read();
                if (done) break;

                // Декодуємо отримані дані
                const text = new TextDecoder().decode(value);
                console.log('📥 Serial received:', text);
                addLog('RX', text.trim());

                // Парсимо через протокол
                if (protocolRef.current) {
                    protocolRef.current.parseMessage(text);
                }
            }
        } catch (error) {
            console.error('Error reading from serial port:', error);
        } finally {
            reader.releaseLock();
            readerRef.current = null;
        }
    };

    // Функція для відправки даних
    const sendData = useCallback(async (message) => {
        if ('serial' in navigator && !window.electronAPI) {
            // Web Serial API
            if (!webSerialPort || !isConnected) {
                console.error('Serial port is not connected');
                return false;
            }

            try {
                if (!writerRef.current && webSerialPort.writable) {
                    console.log('🔧 Getting writer for serial port...');
                    writerRef.current = webSerialPort.writable.getWriter();
                }

                if (!writerRef.current) {
                    console.error('❌ Writer is not available');
                    return false;
                }

                const encoder = new TextEncoder();
                const data = encoder.encode(message);
                console.log('📤 Attempting to write to serial port:', message);
                await writerRef.current.write(data);
                console.log('✅ Serial sent successfully:', message);
                addLog('TX', message.trim());
                return true;
            } catch (error) {
                console.error('❌ Error writing to serial port:', error);
                addLog('TX', message.trim() + ' [ERROR]');
                return false;
            }
        } else if (window.electronAPI) {
            // Electron API
            try {
                console.log('📤 Serial sent (Electron):', message);
                const result = await window.electronAPI.serialWrite(message);
                if (result.success) {
                    addLog('TX', message.trim());
                    return true;
                } else {
                    console.error('❌ Error from Electron API:', result.error);
                    addLog('TX', message.trim() + ' [ERROR: ' + result.error + ']');
                    return false;
                }
            } catch (error) {
                console.error('❌ Error writing to serial port (Electron):', error);
                addLog('TX', message.trim() + ' [ERROR]');
                return false;
            }
        }
        return false;
    }, [webSerialPort, isConnected, addLog]);

    // Функція для відправки команд через протокол
    const sendCommand = useCallback(async (type, params) => {
        console.log('🔵 sendCommand called:', { type, params, isConnected });

        if (!protocolRef.current) {
            console.error('❌ Protocol not initialized');
            return false;
        }

        if (!isConnected) {
            console.error('❌ Serial port is not connected');
            return false;
        }

        let message = '';
        if (type === 'set') {
            // Встановлення параметрів
            message = protocolRef.current.setParameters(params);
            console.log('📝 SET command formatted:', message);
        } else if (type === 'get') {
            // Запит параметрів
            message = protocolRef.current.getParameters(params);
            console.log('📝 GET command formatted:', message);
        }

        if (message) {
            // Додаємо символ нового рядка для коректної роботи серійного порту
            const result = await sendData(message + '\n');
            console.log('📊 sendData result:', result);
            return result;
        }
        console.warn('⚠️ Empty message, nothing to send');
        return false;
    }, [isConnected, sendData]);

    // Реєструємо функцію відправки команд в контексті
    useEffect(() => {
        registerSendCommand(sendCommand);
    }, [registerSendCommand, sendCommand]);

    // Експонуємо функції в window для доступу з консолі
    useEffect(() => {
        // Сира відправка
        window.SerialSend = (message) => {
            sendData(message + '\n');
        };

        // Відправка команди встановлення
        window.SerialSet = (params) => {
            sendCommand('set', params);
        };

        // Відправка команди запиту
        window.SerialGet = (params) => {
            sendCommand('get', params);
        };

        return () => {
            delete window.SerialSend;
            delete window.SerialSet;
            delete window.SerialGet;
        };
    }, [isConnected, webSerialPort]);

    useEffect(() => {
        const loadPorts = async () => {
            // Electron API
            if (window.electronAPI) {
                try {
                    const ports = await window.electronAPI.listSerialPorts();
                    setAvailablePorts(ports);
                } catch (error) {
                    console.error('Error loading serial ports:', error);
                    setAvailablePorts([]);
                }
            }
            // Web Serial API для браузера
            else if ('serial' in navigator) {
                try {
                    const ports = await navigator.serial.getPorts();
                    const portList = ports.map((port, index) => ({
                        value: `port-${index}`,
                        label: `Serial Port ${index + 1}`,
                        port: port
                    }));
                    setAvailablePorts(portList);
                } catch (error) {
                    console.error('Error loading Web Serial ports:', error);
                }
            } else {
                console.warn('Serial API not available');
            }
        };

        loadPorts();

        // Слухаємо події підключення/відключення портів (Web Serial API)
        if ('serial' in navigator) {
            navigator.serial.addEventListener('connect', loadPorts);
            navigator.serial.addEventListener('disconnect', loadPorts);

            return () => {
                navigator.serial.removeEventListener('connect', loadPorts);
                navigator.serial.removeEventListener('disconnect', loadPorts);
            };
        }
    }, []);

    const baudRates = [
        { value: '9600', label: '9600' },
        { value: '19200', label: '19200' },
        { value: '38400', label: '38400' },
        { value: '57600', label: '57600' },
        { value: '115200', label: '115200' },
    ];

    const handleConnect = async () => {
        // Web Serial API
        if ('serial' in navigator && !window.electronAPI) {
            if (isConnected) {
                // Відключення
                readLoopRef.current = false;

                // Чекаємо трохи, щоб reader зупинився
                await new Promise(resolve => setTimeout(resolve, 100));

                if (readerRef.current) {
                    try {
                        await readerRef.current.cancel();
                    } catch (e) {
                        console.warn('Reader cancel error:', e);
                    }
                }

                if (writerRef.current) {
                    try {
                        writerRef.current.releaseLock();
                        writerRef.current = null;
                    } catch (e) {
                        console.warn('Writer release error:', e);
                    }
                }

                if (webSerialPort) {
                    try {
                        await webSerialPort.close();
                        setWebSerialPort(null);
                        setIsConnected(false);
                        console.log('🔌 Serial port disconnected');
                    } catch (error) {
                        console.error('Error closing port:', error);
                    }
                }
            } else {
                // Підключення - запит порту від користувача
                try {
                    const port = await navigator.serial.requestPort();
                    await port.open({ baudRate: parseInt(baudRate) });
                    setWebSerialPort(port);
                    setIsConnected(true);
                    console.log('🔌 Serial port connected');

                    // Запускаємо читання даних
                    startReading(port);

                    // Оновлюємо список портів після підключення нового
                    const ports = await navigator.serial.getPorts();
                    const portList = ports.map((p, index) => ({
                        value: `port-${index}`,
                        label: `Serial Port ${index + 1}`,
                        port: p
                    }));
                    setAvailablePorts(portList);
                } catch (error) {
                    console.error('Error opening port:', error);
                }
            }
        }
        // Electron API
        else if (window.electronAPI) {
            if (!selectedPort) {
                alert('Будь ласка, виберіть порт');
                return;
            }

            if (isConnected) {
                // Відключення
                try {
                    const result = await window.electronAPI.serialClose();
                    if (result.success) {
                        setIsConnected(false);
                        console.log('🔌 Serial port disconnected (Electron)');
                    } else {
                        console.error('Error closing port:', result.error);
                    }
                } catch (error) {
                    console.error('Error closing port:', error);
                }
            } else {
                // Підключення
                try {
                    const result = await window.electronAPI.serialOpen(selectedPort, parseInt(baudRate));
                    if (result.success) {
                        setIsConnected(true);
                        console.log('🔌 Serial port connected (Electron):', selectedPort, '@', baudRate);
                    } else {
                        console.error('Error opening port:', result.error);
                        alert('Помилка підключення: ' + result.error);
                    }
                } catch (error) {
                    console.error('Error opening port:', error);
                    alert('Помилка підключення: ' + error.message);
                }
            }
        }
    };

    return (
        <Stack gap="md">
            {!window.electronAPI && 'serial' in navigator && (
                <Text size="xs" c="dimmed">
                    Web Serial API: натисніть "Підключитись" щоб вибрати порт
                </Text>
            )}

            <Group grow>
                <Select
                    label="Протокол"
                    data={[
                        { value: 'standard', label: 'Standard Protocol' }
                    ]}
                    value={selectedProtocol}
                    onChange={setSelectedProtocol}
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

            <Group grow>
                <Select
                    label="Серійний порт"
                    placeholder="Виберіть порт"
                    data={availablePorts}
                    value={selectedPort}
                    onChange={setSelectedPort}
                    disabled={isConnected || (!window.electronAPI && 'serial' in navigator)}
                />
            </Group>

            <Text size="sm" c="dimmed">
                {isConnected && (webSerialPort || selectedPort)
                    ? `Підключено (${protocolRef.current?.getName()}) зі швидкістю ${baudRate} baud`
                    : 'Підключення не встановлено'}
            </Text>

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
        </Stack>
    );
}

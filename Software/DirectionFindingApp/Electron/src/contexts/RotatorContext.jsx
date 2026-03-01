import React, { createContext, useState, useContext, useCallback, useRef } from 'react';
import { rotatorState } from '../types/rotatorState';

const RotatorContext = createContext();

export function RotatorProvider({ children }) {
    const [angles, setAngles] = useState(rotatorState);
    const [settings, setSettings] = useState({
        speed: 0,
        minSpeed: 140,
        maxSpeed: 255,
        tolerance: 1.0,
        brake: 15.0,
        initMode: 0,
    });

    // Референс на функцію відправки команд (буде встановлено Connector'ом)
    const sendCommandRef = useRef(null);
    const getSettingsParamsRef = useRef(() => []);

    /**
     * Реєструє функцію відправки команд від Connector
     */
    const registerSendCommand = useCallback((sendFn) => {
        sendCommandRef.current = sendFn;
    }, []);

    /**
     * Реєструє функцію отримання параметрів налаштувань від протоколу
     */
    const registerGetSettingsParams = useCallback((getParamsFn) => {
        getSettingsParamsRef.current = getParamsFn || (() => []);
    }, []);

    /**
     * Оновлює дані ротатора з розпарсеного повідомлення
     * @param {Object} data - Об'єкт з полями: currentAzimuth, currentElevation, targetAzimuth, targetElevation
     */
    const updateRotatorData = useCallback((data) => {
        setAngles(prev => ({
            ...prev,
            ...data
        }));
    }, []);

    /**
     * Оновлює налаштування ротатора
     * @param {Object} data - Об'єкт з полями налаштувань
     */
    const updateSettings = useCallback((data) => {
        setSettings(prev => ({
            ...prev,
            ...data
        }));
    }, []);

    /**
     * Встановлює цільовий азимут
     */
    const setTargetAzimuth = useCallback((azimuth) => {
        setAngles(prev => ({ ...prev, targetAzimuth: azimuth }));
    }, []);

    /**
     * Встановлює цільовий кут нахилу
     */
    const setTargetElevation = useCallback((elevation) => {
        setAngles(prev => ({ ...prev, targetElevation: elevation }));
    }, []);

    /**
     * Відправляє команду на встановлення параметрів
     * @param {Object} params - Об'єкт з параметрами для встановлення
     */
    const sendSetCommand = useCallback((params) => {
        if (sendCommandRef.current) {
            sendCommandRef.current('set', params);
        }
    }, []);

    /**
     * Відправляє команду на запит параметрів
     * @param {string[]} params - Масив назв параметрів для запиту
     */
    const sendGetCommand = useCallback((params) => {
        if (sendCommandRef.current) {
            sendCommandRef.current('get', params);
        }
    }, []);

    /**
     * Повертає список параметрів налаштувань для поточного протоколу
     */
    const getSettingsParams = useCallback(() => {
        return getSettingsParamsRef.current ? getSettingsParamsRef.current() : [];
    }, []);

    const value = {
        angles,
        settings,
        setAngles,
        setSettings,
        updateRotatorData,
        updateSettings,
        setTargetAzimuth,
        setTargetElevation,
        sendSetCommand,
        sendGetCommand,
        getSettingsParams,
        registerSendCommand,
        registerGetSettingsParams,
    };

    return <RotatorContext.Provider value={value}>{children}</RotatorContext.Provider>;
}

export function useRotator() {
    const context = useContext(RotatorContext);
    if (!context) {
        throw new Error('useRotator must be used within RotatorProvider');
    }
    return context;
}

import React, { createContext, useState, useContext, useCallback } from 'react';

const LoggingContext = createContext();

export function LoggingProvider({ children }) {
    const [logs, setLogs] = useState([]);

    const addLog = useCallback((direction, message) => {
        const now = new Date();
        const timestamp = `[${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}.${now.getMilliseconds().toString().padStart(3, '0')}]`;

        setLogs(prev => [...prev, {
            id: Date.now() + Math.random(),
            timestamp,
            direction, // 'TX' or 'RX'
            message
        }]);
    }, []);

    const clearLogs = useCallback(() => {
        setLogs([]);
    }, []);

    const value = {
        logs,
        addLog,
        clearLogs
    };

    return <LoggingContext.Provider value={value}>{children}</LoggingContext.Provider>;
}

export function useLogging() {
    const context = useContext(LoggingContext);
    if (!context) {
        throw new Error('useLogging must be used within LoggingProvider');
    }
    return context;
}

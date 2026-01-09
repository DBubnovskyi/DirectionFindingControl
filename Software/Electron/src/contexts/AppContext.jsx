import React, { createContext, useState, useContext } from 'react';
import { rotatorState } from '../types/rotatorState';

const AppContext = createContext();

export function AppProvider({ children }) {
    const [angles, setAngles] = useState(rotatorState);

    const value = {
        angles,
        setAngles,
    };

    return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp() {
    const context = useContext(AppContext);
    if (!context) {
        throw new Error('useApp must be used within AppProvider');
    }
    return context;
}
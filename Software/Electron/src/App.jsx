import React from 'react';
import { AppShell } from '@mantine/core';
import { IconPlugConnected, IconAdjustmentsDown, IconCompass } from '@tabler/icons-react';
import './App.css';
import Header from './components/Header/Header';
import SerialPortSelector from './components/SerialPortSelector/SerialPortSelector';
import SerialControl from './components/SerialControl/SerialControl';
import GridBlock from './components/GridBlock/GridBlock';
import { AppProvider } from './contexts/AppContext';
import Initialization from './components/Initialization/Initialization';

export default function App() {
    return (
        <AppProvider>
            <AppShell>
                <Header />
                <GridBlock>
                    <SerialPortSelector
                        title={'Підключення'}
                        icon={IconPlugConnected}
                        defaultSize={{ cols: 15, rows: 7 }}
                    />
                    <Initialization
                        title={'Ініціалізація'}
                        icon={IconAdjustmentsDown}
                        defaultSize={{ cols: 15, rows: 7 }}
                    />
                    <SerialControl
                        title={'Керування'}
                        icon={IconCompass}
                        defaultSize={{ cols: 15, rows: 17 }}
                    />
                </GridBlock>
            </AppShell>
        </AppProvider>
    );
}
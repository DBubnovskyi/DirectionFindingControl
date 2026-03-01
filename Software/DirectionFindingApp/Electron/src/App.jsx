import React from 'react';
import { AppShell } from '@mantine/core';
import { IconPlugConnected, IconAdjustmentsDown, IconCompass, IconMap, IconFileText, IconSettings } from '@tabler/icons-react';
import './App.css';
import Header from './components/Header/Header';
import Connector from './components/Connector/Connector';
import CompassControl from './components/CompassControl/CompassControl';
import GridBlock from './components/GridBlock/GridBlock';
import { AppProvider } from './contexts/AppContext';
import { RotatorProvider } from './contexts/RotatorContext';
import { LoggingProvider } from './contexts/LoggingContext';
import Initialization from './components/Initialization/Initialization';
import Logging from './components/Logging/Logging';
import Settings from './components/Settings/Settings';
import TopographyMap from './components/SVG/TopographyMap';
import Map from './components/Map/Map';

export default function App() {
    return (
        <AppProvider>
            <RotatorProvider>
                <LoggingProvider>
                    <div className="app-background">
                        <TopographyMap />
                    </div>
                    <AppShell>
                        <Header />
                        <GridBlock>
                            <Connector
                                title={'Підключення'}
                                icon={IconPlugConnected}
                                defaultSize={{ cols: 15, rows: 8 }}
                            />
                            <Initialization
                                title={'Ініціалізація'}
                                icon={IconAdjustmentsDown}
                                defaultSize={{ cols: 15, rows: 7 }}
                            />
                            <Logging
                                title={'Логування'}
                                icon={IconFileText}
                                defaultSize={{ cols: 15, rows: 7 }}
                            />
                            <Settings
                                title={'Налаштування'}
                                icon={IconSettings}
                                defaultSize={{ cols: 15, rows: 12 }}
                            />
                            <CompassControl
                                title={'Керування'}
                                icon={IconCompass}
                                defaultSize={{ cols: 15, rows: 17 }}
                            />
                            <Map
                                title={'Карта'}
                                icon={IconMap}
                                defaultSize={{ cols: 40, rows: 25 }}
                            />
                        </GridBlock>
                    </AppShell>
                </LoggingProvider>
            </RotatorProvider>
        </AppProvider>
    );
}
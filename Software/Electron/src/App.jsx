import React, { useState, useEffect } from 'react';
import { AppShell } from '@mantine/core';
import { IconPlugConnected, IconAdjustmentsDown, IconCompass } from '@tabler/icons-react';
import './App.css';
import Header from './components/Header/Header';
import SerialPortSelector from './components/SerialPortSelector/SerialPortSelector';
import SerialControl from './components/SerialControl/SerialControl';
import GridBlock from './components/GridBlock/GridBlock';

const BLOCKS = [
    { id: 0, title: 'Підключення', icon: IconPlugConnected, component: SerialPortSelector },
    { id: 1, title: 'Ініціалізація', icon: IconAdjustmentsDown, component: 'div' },
    { id: 2, title: 'Керування', icon: IconCompass, component: SerialControl }
];

const VISIBILITY_KEY = 'gridBlockVisibility';
const LAYOUTS_KEY = 'savedLayouts';
const ACTIVE_LAYOUT_KEY = 'activeLayoutId';

export default function App() {
    const [visibleBlocks, setVisibleBlocks] = useState(() => {
        try {
            const saved = localStorage.getItem(VISIBILITY_KEY);
            if (saved) {
                return JSON.parse(saved);
            }
        } catch (error) {
            console.error('Error loading visibility from localStorage:', error);
        }
        return [0, 1, 2]; // За замовчуванням всі видимі
    });

    const [currentLayout, setCurrentLayout] = useState(null);
    const [layoutToLoad, setLayoutToLoad] = useState(null);
    const [activeLayoutId, setActiveLayoutId] = useState(() => {
        try {
            const saved = localStorage.getItem(ACTIVE_LAYOUT_KEY);
            if (saved) {
                return JSON.parse(saved);
            }
        } catch (error) {
            console.error('Error loading active layout from localStorage:', error);
        }
        return null;
    });
    const [savedLayouts, setSavedLayouts] = useState(() => {
        try {
            const saved = localStorage.getItem(LAYOUTS_KEY);
            if (saved) {
                return JSON.parse(saved);
            }
        } catch (error) {
            console.error('Error loading layouts from localStorage:', error);
        }
        return [];
    });

    useEffect(() => {
        try {
            localStorage.setItem(VISIBILITY_KEY, JSON.stringify(visibleBlocks));
        } catch (error) {
            console.error('Error saving visibility to localStorage:', error);
        }
    }, [visibleBlocks]);

    useEffect(() => {
        try {
            localStorage.setItem(LAYOUTS_KEY, JSON.stringify(savedLayouts));
        } catch (error) {
            console.error('Error saving layouts to localStorage:', error);
        }
    }, [savedLayouts]);

    useEffect(() => {
        try {
            localStorage.setItem(ACTIVE_LAYOUT_KEY, JSON.stringify(activeLayoutId));
        } catch (error) {
            console.error('Error saving active layout to localStorage:', error);
        }
    }, [activeLayoutId]);

    const hideBlock = (blockId) => {
        setVisibleBlocks(prev => prev.filter(id => id !== blockId));
    };

    const showBlock = (blockId) => {
        setVisibleBlocks(prev => [...prev, blockId]);
    };

    const saveLayout = (name) => {
        if (!name || !currentLayout) return;
        const newLayout = {
            id: Date.now(),
            name,
            visibleBlocks: [...visibleBlocks],
            gridLayout: { ...currentLayout }
        };
        setSavedLayouts(prev => [...prev, newLayout]);
    };

    const loadLayout = (layoutId) => {
        console.log('Loading layout:', layoutId);
        const layout = savedLayouts.find(l => l.id === layoutId);
        if (!layout) return;
        setVisibleBlocks(layout.visibleBlocks);
        setLayoutToLoad(layout.gridLayout);
        setActiveLayoutId(layoutId);
    };

    const deleteLayout = (layoutId) => {
        setSavedLayouts(prev => prev.filter(l => l.id !== layoutId));
    };

    const hiddenBlocks = BLOCKS.filter(block => !visibleBlocks.includes(block.id));

    return (
        <AppShell>
            <Header
                hiddenBlocks={hiddenBlocks}
                onShowBlock={showBlock}
                savedLayouts={savedLayouts}
                onSaveLayout={saveLayout}
                onLoadLayout={loadLayout}
                onDeleteLayout={deleteLayout}
                currentLayout={currentLayout}
                activeLayoutId={activeLayoutId} />
            <GridBlock
                onHideBlock={hideBlock}
                onLayoutChange={setCurrentLayout}
                layoutToLoad={layoutToLoad}
                onLayoutLoaded={() => setLayoutToLoad(null)}
            >
                {BLOCKS.filter(block => visibleBlocks.includes(block.id)).map(block => {
                    const Component = block.component;
                    if (Component === 'div') {
                        return <div key={block.id} blockId={block.id} title={block.title} icon={block.icon}>
                            {/* Тут можна додати компоненти для ініціалізації */}
                        </div>;
                    }
                    return <Component key={block.id} blockId={block.id} title={block.title} icon={block.icon} />;
                })}
            </GridBlock>
        </AppShell>
    );
}
import React, { createContext, useState, useContext, useEffect } from 'react';
import { rotatorState } from '../types/rotatorState';

const AppContext = createContext();

const VISIBILITY_KEY = 'gridBlockVisibility';
const LAYOUTS_KEY = 'savedLayouts';
const ACTIVE_LAYOUT_KEY = 'activeLayoutId';

export function AppProvider({ children }) {
    const [angles, setAngles] = useState(rotatorState);

    // Збираємо blocks з children GridBlock
    const blocks = React.useMemo(() => {
        let gridBlockChildren = [];

        const findGridBlock = (element) => {
            if (!element) return;

            if (element.type?.name === 'GridBlock') {
                gridBlockChildren = React.Children.toArray(element.props.children);
                return;
            }

            if (element.props?.children) {
                React.Children.forEach(element.props.children, findGridBlock);
            }
        };

        findGridBlock(children);

        const result = gridBlockChildren.map((child, index) => ({
            id: index,
            title: child.props.title,
            icon: child.props.icon,
            defaultSize: child.props.defaultSize
        }));

        console.log('AppContext blocks:', result);
        return result;
    }, [children]);

    const [visibleBlocks, setVisibleBlocks] = useState(() => {
        try {
            const saved = localStorage.getItem(VISIBILITY_KEY);
            if (saved) return JSON.parse(saved);
        } catch { }
        return []; // Початково порожній, заповниться в useEffect
    });

    // Синхронізувати visibleBlocks з blocks
    useEffect(() => {
        if (blocks.length > 0 && visibleBlocks.length === 0) {
            const saved = localStorage.getItem(VISIBILITY_KEY);
            if (!saved) {
                setVisibleBlocks(blocks.map(b => b.id));
            }
        }
    }, [blocks]);
    const [currentLayout, setCurrentLayout] = useState(null);
    const [layoutToLoad, setLayoutToLoad] = useState(null);
    const [activeLayoutId, setActiveLayoutId] = useState(() => {
        try {
            const saved = localStorage.getItem(ACTIVE_LAYOUT_KEY);
            if (saved) return JSON.parse(saved);
        } catch { }
        return null;
    });
    const [savedLayouts, setSavedLayouts] = useState(() => {
        try {
            const saved = localStorage.getItem(LAYOUTS_KEY);
            if (saved) return JSON.parse(saved);
        } catch { }
        return [];
    });

    useEffect(() => {
        try { localStorage.setItem(VISIBILITY_KEY, JSON.stringify(visibleBlocks)); } catch { }
    }, [visibleBlocks]);
    useEffect(() => {
        try { localStorage.setItem(LAYOUTS_KEY, JSON.stringify(savedLayouts)); } catch { }
    }, [savedLayouts]);
    useEffect(() => {
        try { localStorage.setItem(ACTIVE_LAYOUT_KEY, JSON.stringify(activeLayoutId)); } catch { }
    }, [activeLayoutId]);

    const hideBlock = (blockId) => setVisibleBlocks(prev => prev.filter(id => id !== blockId));
    const showBlock = (blockId) => setVisibleBlocks(prev => [...prev, blockId]);
    const saveLayout = (name) => {
        if (!name || !currentLayout) return;
        const newLayout = { id: Date.now(), name, visibleBlocks: [...visibleBlocks], gridLayout: { ...currentLayout } };
        setSavedLayouts(prev => [...prev, newLayout]);
    };
    const loadLayout = (layoutId) => {
        const layout = savedLayouts.find(l => l.id === layoutId);
        if (!layout) return;
        setVisibleBlocks(layout.visibleBlocks);
        setLayoutToLoad(layout.gridLayout);
        setActiveLayoutId(layoutId);
    };
    const deleteLayout = (layoutId) => {
        setSavedLayouts(prev => prev.filter(l => l.id !== layoutId));
        if (activeLayoutId === layoutId) setActiveLayoutId(null);
    };

    const hiddenBlocks = blocks.filter(b => !visibleBlocks.includes(b.id));

    const value = {
        // blocks and layout mgmt
        blocks,
        visibleBlocks,
        hiddenBlocks,
        savedLayouts,
        activeLayoutId,
        currentLayout,
        layoutToLoad,
        setCurrentLayout,
        setLayoutToLoad,
        hideBlock,
        showBlock,
        saveLayout,
        loadLayout,
        deleteLayout,
        // existing angles
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
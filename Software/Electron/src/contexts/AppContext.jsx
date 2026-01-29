import React, { createContext, useState, useContext, useEffect, useCallback } from 'react';
import { rotatorState } from '../types/rotatorState';

const AppContext = createContext();

const VISIBILITY_KEY = 'gridBlockVisibility';
const LAYOUTS_KEY = 'savedLayouts';
const ACTIVE_LAYOUT_KEY = 'activeLayoutId';
const COLLAPSED_KEY = 'gridBlockCollapsed';
const EDIT_MODE_KEY = 'gridBlockEditMode';

export function AppProvider({ children }) {
    const [angles, setAngles] = useState(rotatorState);
    const [blocks, setBlocks] = useState([]);
    const [visibleBlocks, setVisibleBlocks] = useState(() => {
        try {
            const saved = localStorage.getItem(VISIBILITY_KEY);
            return saved ? JSON.parse(saved) : [];
        } catch {
            return [];
        }
    });
    const [currentLayout, setCurrentLayout] = useState(null);
    const [layoutToLoad, setLayoutToLoad] = useState(null);
    const [activeLayoutId, setActiveLayoutId] = useState(() => {
        try {
            const saved = localStorage.getItem(ACTIVE_LAYOUT_KEY);
            return saved ? JSON.parse(saved) : null;
        } catch {
            return null;
        }
    });
    const [savedLayouts, setSavedLayouts] = useState(() => {
        try {
            const saved = localStorage.getItem(LAYOUTS_KEY);
            return saved ? JSON.parse(saved) : [];
        } catch {
            return [];
        }
    });
    const [collapsedBlocks, setCollapsedBlocks] = useState(() => {
        try {
            const saved = localStorage.getItem(COLLAPSED_KEY);
            return saved ? JSON.parse(saved) : [];
        } catch {
            return [];
        }
    });
    const [isEditMode, setIsEditMode] = useState(() => {
        try {
            const saved = localStorage.getItem(EDIT_MODE_KEY);
            return saved ? JSON.parse(saved) : true;
        } catch {
            return true;
        }
    });

    // Реєстрація блоків з GridBlock
    const registerBlocks = useCallback((blocksData) => {
        setBlocks(blocksData);
        if (visibleBlocks.length === 0) {
            const saved = localStorage.getItem(VISIBILITY_KEY);
            if (!saved) {
                setVisibleBlocks(blocksData.map(b => b.id));
            }
        }
    }, [visibleBlocks.length]);

    // Збереження стану в localStorage
    useEffect(() => {
        if (visibleBlocks.length > 0) {
            localStorage.setItem(VISIBILITY_KEY, JSON.stringify(visibleBlocks));
        }
    }, [visibleBlocks]);

    useEffect(() => {
        localStorage.setItem(LAYOUTS_KEY, JSON.stringify(savedLayouts));
    }, [savedLayouts]);

    useEffect(() => {
        localStorage.setItem(ACTIVE_LAYOUT_KEY, JSON.stringify(activeLayoutId));
    }, [activeLayoutId]);

    useEffect(() => {
        localStorage.setItem(COLLAPSED_KEY, JSON.stringify(collapsedBlocks));
    }, [collapsedBlocks]);

    useEffect(() => {
        localStorage.setItem(EDIT_MODE_KEY, JSON.stringify(isEditMode));
    }, [isEditMode]);

    // Управління блоками
    const hideBlock = useCallback((blockId) => {
        setVisibleBlocks(prev => prev.filter(id => id !== blockId));
    }, []);

    const showBlock = useCallback((blockId) => {
        setVisibleBlocks(prev => [...prev, blockId]);
    }, []);

    const toggleCollapse = useCallback((blockId) => {
        setCollapsedBlocks(prev =>
            prev.includes(blockId)
                ? prev.filter(id => id !== blockId)
                : [...prev, blockId]
        );
    }, []);

    const toggleEditMode = useCallback(() => {
        setIsEditMode(prev => !prev);
    }, []);

    // Управління лейаутами
    const saveLayout = useCallback((name) => {
        if (!name || !currentLayout) return;
        const newLayout = {
            id: Date.now(),
            name,
            visibleBlocks: [...visibleBlocks],
            gridLayout: { ...currentLayout }
        };
        setSavedLayouts(prev => [...prev, newLayout]);
    }, [currentLayout, visibleBlocks]);

    const loadLayout = useCallback((layoutId) => {
        const layout = savedLayouts.find(l => l.id === layoutId);
        if (!layout) return;

        // Завжди завантажуємо лейаут, навіть якщо він вже активний
        setVisibleBlocks([...layout.visibleBlocks]);
        setLayoutToLoad({ ...layout.gridLayout });
        setActiveLayoutId(layoutId);
    }, [savedLayouts]);

    const deleteLayout = useCallback((layoutId) => {
        setSavedLayouts(prev => prev.filter(l => l.id !== layoutId));
        if (activeLayoutId === layoutId) {
            setActiveLayoutId(null);
        }
    }, [activeLayoutId]);

    // Обчислення прихованих блоків
    const hiddenBlocks = React.useMemo(() => {
        return blocks.filter(b => !visibleBlocks.includes(b.id));
    }, [blocks, visibleBlocks]);

    const value = {
        blocks,
        visibleBlocks,
        hiddenBlocks,
        collapsedBlocks,
        isEditMode,
        savedLayouts,
        activeLayoutId,
        currentLayout,
        layoutToLoad,
        setCurrentLayout,
        setLayoutToLoad,
        registerBlocks,
        hideBlock,
        showBlock,
        toggleCollapse,
        toggleEditMode,
        saveLayout,
        loadLayout,
        deleteLayout,
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
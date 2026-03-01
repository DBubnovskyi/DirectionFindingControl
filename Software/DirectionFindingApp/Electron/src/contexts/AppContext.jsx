import React, { createContext, useState, useContext, useEffect, useCallback } from 'react';

const AppContext = createContext();

const VISIBILITY_KEY = 'gridBlockVisibility';
const LAYOUTS_KEY = 'savedLayouts';
const ACTIVE_LAYOUT_KEY = 'activeLayoutId';
const COLLAPSED_KEY = 'gridBlockCollapsed';
const EXPANDED_ROWS_KEY = 'gridBlockExpandedRows';
const EDIT_MODE_KEY = 'gridBlockEditMode';
const LAYOUT_PARAM_KEY = 'layout';

export function AppProvider({ children }) {
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
    const [expandedRows, setExpandedRows] = useState(() => {
        try {
            const saved = localStorage.getItem(EXPANDED_ROWS_KEY);
            return saved ? JSON.parse(saved) : {};
        } catch {
            return {};
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

    const setLayoutParam = useCallback((name) => {
        try {
            const url = new URL(window.location.href);
            if (name) {
                url.searchParams.set(LAYOUT_PARAM_KEY, name);
            } else {
                url.searchParams.delete(LAYOUT_PARAM_KEY);
            }
            window.history.replaceState({}, '', url.toString());
        } catch {
            // no-op
        }
    }, []);

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
        localStorage.setItem(EXPANDED_ROWS_KEY, JSON.stringify(expandedRows));
    }, [expandedRows]);

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

    const setExpandedRow = useCallback((blockId, rows) => {
        if (!blockId && blockId !== 0) return;
        if (!rows) return;
        setExpandedRows(prev => ({
            ...prev,
            [blockId]: rows
        }));
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
            gridLayout: { ...currentLayout },
            collapsedBlocks: [...collapsedBlocks],
            expandedRows: { ...expandedRows }
        };
        setSavedLayouts(prev => [...prev, newLayout]);
    }, [currentLayout, visibleBlocks, collapsedBlocks, expandedRows]);

    const loadLayout = useCallback((layoutId) => {
        const layout = savedLayouts.find(l => l.id === layoutId);
        if (!layout) return;

        // Завжди завантажуємо лейаут, навіть якщо він вже активний
        setVisibleBlocks([...layout.visibleBlocks]);
        setLayoutToLoad({ ...layout.gridLayout });
        setCollapsedBlocks([...(layout.collapsedBlocks || [])]);
        setExpandedRows({ ...(layout.expandedRows || {}) });
        setActiveLayoutId(layoutId);
        setLayoutParam(layout.name);
    }, [savedLayouts, setLayoutParam]);

    const loadLayoutByName = useCallback((name) => {
        if (!name) return;
        const layout = savedLayouts.find(l => l.name === name);
        if (!layout) return;
        loadLayout(layout.id);
    }, [savedLayouts, loadLayout]);

    const deleteLayout = useCallback((layoutId) => {
        setSavedLayouts(prev => prev.filter(l => l.id !== layoutId));
        if (activeLayoutId === layoutId) {
            setActiveLayoutId(null);
            setLayoutParam(null);
        }
    }, [activeLayoutId, setLayoutParam]);

    const updateLayout = useCallback((layoutId, updates) => {
        if (!layoutId || !updates) return;
        setSavedLayouts(prev =>
            prev.map(layout => {
                if (layout.id !== layoutId) return layout;
                if (layout.id === activeLayoutId && updates.name) {
                    setLayoutParam(updates.name);
                }
                return {
                    ...layout,
                    name: updates.name !== undefined ? updates.name : layout.name,
                    gridLayout: updates.gridLayout !== undefined ? updates.gridLayout : layout.gridLayout,
                    visibleBlocks: updates.visibleBlocks !== undefined ? updates.visibleBlocks : layout.visibleBlocks,
                    collapsedBlocks: updates.collapsedBlocks !== undefined ? updates.collapsedBlocks : layout.collapsedBlocks,
                    expandedRows: updates.expandedRows !== undefined ? updates.expandedRows : layout.expandedRows,
                };
            })
        );
    }, [activeLayoutId, setLayoutParam]);

    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const layoutName = params.get(LAYOUT_PARAM_KEY);
        if (layoutName) {
            loadLayoutByName(layoutName);
        }
    }, [savedLayouts, loadLayoutByName]);

    // Обчислення прихованих блоків
    const hiddenBlocks = React.useMemo(() => {
        return blocks.filter(b => !visibleBlocks.includes(b.id));
    }, [blocks, visibleBlocks]);

    const value = {
        blocks,
        visibleBlocks,
        hiddenBlocks,
        collapsedBlocks,
        expandedRows,
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
        setExpandedRow,
        toggleEditMode,
        saveLayout,
        loadLayout,
        loadLayoutByName,
        deleteLayout,
        updateLayout,
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
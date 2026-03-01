import React, { createContext, useState, useContext, useEffect, useCallback } from 'react';
import { rotatorState } from '../types/rotatorState';

const AppContext = createContext();

const VISIBILITY_KEY = 'gridBlockVisibility';
const LAYOUTS_KEY = 'savedLayouts';
const ACTIVE_LAYOUT_KEY = 'activeLayoutId';
const COLLAPSED_KEY = 'gridBlockCollapsed';
const EXPANDED_ROWS_KEY = 'gridBlockExpandedRows';
const EDIT_MODE_KEY = 'gridBlockEditMode';
const LAYOUT_PARAM_KEY = 'layout';

function resolveApiBase() {
    const envBase = process.env.REACT_APP_API_BASE;
    if (envBase && envBase.trim()) {
        return envBase.trim().replace(/\/$/, '');
    }
    return '/api/rotator';
}

const API_BASE = resolveApiBase();
const STATE_ENDPOINT = API_BASE;
const SETTINGS_ENDPOINT = `${API_BASE}/settings`;
const LOGS_CLEAR_ENDPOINT = `${API_BASE}/logs/clear`;

const defaultSettings = {
    speed: 0,
    minSpeed: 70,
    maxSpeed: 100,
    tolerance: 1.0,
    brake: 15.0,
    initMode: 0,
};

function normalizeServerState(payload) {
    const source = payload || {};
    const sourceAngles = source.angles || source.rotator || {};
    const sourceSettings = source.settings || {};
    const sourceConnection = source.connection || {};

    return {
        angles: {
            ...rotatorState,
            ...sourceAngles,
            currentAngle: sourceAngles.currentAngle ?? sourceAngles.currentElevation ?? source.currentAngle ?? rotatorState.currentAngle,
            targetAngle: sourceAngles.targetAngle ?? sourceAngles.targetElevation ?? source.targetAngle ?? rotatorState.targetAngle,
            currentAzimuth: sourceAngles.currentAzimuth ?? source.currentAzimuth ?? rotatorState.currentAzimuth,
            targetAzimuth: sourceAngles.targetAzimuth ?? source.targetAzimuth ?? rotatorState.targetAzimuth,
        },
        settings: {
            ...defaultSettings,
            ...sourceSettings,
        },
        logs: Array.isArray(source.logs) ? source.logs : [],
        connection: {
            isConnected: Boolean(sourceConnection.isConnected ?? source.isConnected ?? false),
            status: sourceConnection.status || source.status || 'unknown',
        },
    };
}

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
    const [angles, setAngles] = useState(rotatorState);
    const [settings, setSettings] = useState(defaultSettings);
    const [logs, setLogs] = useState([]);
    const [connection, setConnection] = useState({ isConnected: false, status: 'loading' });
    const [isServerLoading, setIsServerLoading] = useState(true);
    const [isSavingSettings, setIsSavingSettings] = useState(false);
    const [serverError, setServerError] = useState('');
    const [lastServerUpdate, setLastServerUpdate] = useState(null);

    const refreshServerState = useCallback(async () => {
        try {
            const response = await fetch(STATE_ENDPOINT, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const payload = await response.json();
            const normalized = normalizeServerState(payload);
            console.log('Normalized server state:', normalized);
            console.log('Raw server response:', payload);

            setAngles(normalized.angles);
            setSettings(normalized.settings);
            setLogs(payload.connection.portLog);
            setConnection(normalized.connection);
            setServerError('');
            setLastServerUpdate(new Date());
        } catch (error) {
            setServerError(error?.message || 'Failed to load server state');
            setConnection(prev => ({ ...prev, isConnected: false }));
            // setLogs(prevLogs => [
            //     ...prevLogs,
            //     createLogEntry('', `API error: ${error?.message || 'Failed to load server state'}`)
            // ]);
        } finally {
            setIsServerLoading(false);
        }
    }, []);

    useEffect(() => {
        refreshServerState();
        const timerId = setInterval(() => {
            refreshServerState();
        }, 2000);

        return () => clearInterval(timerId);
    }, [refreshServerState]);

    const saveSettingsToServer = useCallback(async (nextSettings) => {
        if (!nextSettings) return false;

        setIsSavingSettings(true);
        try {
            const response = await fetch(SETTINGS_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                },
                body: JSON.stringify(nextSettings),
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            await refreshServerState();
            return true;
        } catch (error) {
            setServerError(error?.message || 'Failed to save settings');
            return false;
        } finally {
            setIsSavingSettings(false);
        }
    }, [refreshServerState]);

    const clearLogs = useCallback(async () => {
        try {
            const response = await fetch(LOGS_CLEAR_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            await refreshServerState();
        } catch {
            setLogs([]);
        }
    }, [refreshServerState]);

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
        angles,
        settings,
        logs,
        connection,
        isServerLoading,
        isSavingSettings,
        serverError,
        lastServerUpdate,
        refreshServerState,
        saveSettingsToServer,
        clearLogs,
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
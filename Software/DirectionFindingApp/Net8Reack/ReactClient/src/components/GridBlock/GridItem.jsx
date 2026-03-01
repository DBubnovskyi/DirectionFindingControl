import React, { useRef } from 'react';
import { Paper, Title, ActionIcon } from '@mantine/core';
import { IconX, IconChevronDown, IconChevronUp } from '@tabler/icons-react';
import './GridBlock.scss';
import { useApp } from '../../contexts/AppContext';

export default function GridItem({ blockId, title, icon: Icon, children, layout, onLayoutChange, onDragPreview, onDragEnd, onHide, isVisible = true, headerActions }) {
    const { collapsedBlocks, expandedRows, toggleCollapse, setExpandedRow, isEditMode } = useApp();
    const isCollapsed = collapsedBlocks.includes(blockId);

    const resizingRef = useRef(false);
    const draggingRef = useRef(false);
    const startPosRef = useRef({ x: 0, y: 0 });
    const startSizeRef = useRef({ cols: 0, rows: 0 });
    const startGridPosRef = useRef({ col: 0, row: 0 });
    const currentSizeRef = useRef({ cols: layout?.cols || 0, rows: layout?.rows || 0 });
    const currentPosRef = useRef({ col: layout?.col || 0, row: layout?.row || 0 });
    const originalRowsRef = useRef(layout?.rows || 0);
    const blockRef = useRef(null);

    // Оновлюємо refs при зміні layout
    React.useEffect(() => {
        if (!layout) return;
        currentSizeRef.current = { cols: layout.cols, rows: layout.rows };
        currentPosRef.current = { col: layout.col, row: layout.row };
        // Зберігаємо оригінальну висоту тільки якщо блок не згорнутий
        if (!isCollapsed && layout.rows > 2) {
            originalRowsRef.current = layout.rows;
            setExpandedRow(blockId, layout.rows);
        }
    }, [layout, isCollapsed, blockId, setExpandedRow]);

    // Захист від undefined layout
    if (!layout) {
        return null;
    }

    const handleResizeStart = (e) => {
        if (!isEditMode) return; // Блокуємо resize якщо не в режимі редагування
        e.preventDefault();
        e.stopPropagation();
        resizingRef.current = true;
        startPosRef.current = { x: e.clientX, y: e.clientY };
        startSizeRef.current = { cols: layout.cols, rows: layout.rows };
        currentSizeRef.current = { cols: layout.cols, rows: layout.rows };

        document.addEventListener('mousemove', handleResizeMove);
        document.addEventListener('mouseup', handleResizeEnd);
    };

    const handleResizeMove = (e) => {
        if (!resizingRef.current) return;

        const deltaX = e.clientX - startPosRef.current.x;
        const deltaY = e.clientY - startPosRef.current.y;

        const gridGap = 5;
        const colWidth = 19;
        const rowHeight = 19;

        // Враховуємо gap між клітинками
        const deltaCols = Math.round(deltaX / (colWidth + gridGap));
        const deltaRows = Math.round(deltaY / (rowHeight + gridGap));

        const newCols = Math.max(1, Math.min(96, startSizeRef.current.cols + deltaCols));
        const newRows = Math.max(1, startSizeRef.current.rows + deltaRows);

        currentSizeRef.current = { cols: newCols, rows: newRows };

        if (blockRef.current) {
            blockRef.current.style.gridColumn = `${layout.col} / span ${newCols}`;
            blockRef.current.style.gridRow = `${layout.row} / span ${newRows}`;
        }
    };

    const handleResizeEnd = () => {
        resizingRef.current = false;
        document.removeEventListener('mousemove', handleResizeMove);
        document.removeEventListener('mouseup', handleResizeEnd);

        onLayoutChange({ cols: currentSizeRef.current.cols, rows: currentSizeRef.current.rows });
        if (!isCollapsed && currentSizeRef.current.rows > 2) {
            setExpandedRow(blockId, currentSizeRef.current.rows);
        }
    };

    const handleDragStart = (e) => {
        if (!isEditMode) return; // Блокуємо drag якщо не в режимі редагування
        if (e.target.closest('.resize-handle')) return;

        e.preventDefault();
        draggingRef.current = true;
        startPosRef.current = { x: e.clientX, y: e.clientY };
        startGridPosRef.current = { col: layout.col, row: layout.row };
        currentPosRef.current = { col: layout.col, row: layout.row };

        document.addEventListener('mousemove', handleDragMove);
        document.addEventListener('mouseup', handleDragEndHandler);

        if (blockRef.current) {
            blockRef.current.style.opacity = '0.5';
            blockRef.current.style.zIndex = '1000';
        }
    };

    const handleDragMove = (e) => {
        if (!draggingRef.current) return;

        const deltaX = e.clientX - startPosRef.current.x;
        const deltaY = e.clientY - startPosRef.current.y;

        const gridGap = 5;
        const colWidth = 19;
        const rowHeight = 19;

        // Плавне переміщення через transform
        if (blockRef.current) {
            blockRef.current.style.transform = `translate(${deltaX}px, ${deltaY}px)`;
        }

        // Враховуємо gap між клітинками для preview
        const deltaCols = Math.round(deltaX / (colWidth + gridGap));
        const deltaRows = Math.round(deltaY / (rowHeight + gridGap));

        const newCol = Math.max(1, Math.min(97 - layout.cols, startGridPosRef.current.col + deltaCols));
        const newRow = Math.max(1, startGridPosRef.current.row + deltaRows);

        currentPosRef.current = { col: newCol, row: newRow };

        // Показуємо placeholder де буде блок після компактування
        if (onDragPreview) {
            onDragPreview(currentPosRef.current);
        }
    };

    const handleDragEndHandler = () => {
        draggingRef.current = false;
        document.removeEventListener('mousemove', handleDragMove);
        document.removeEventListener('mouseup', handleDragEndHandler);

        // Скидаємо transform
        if (blockRef.current) {
            blockRef.current.style.transform = '';
            blockRef.current.style.opacity = '1';
            blockRef.current.style.zIndex = 'auto';
        }

        if (onDragEnd) {
            onDragEnd();
        }

        // Оновлюємо grid position тільки в кінці drag
        onLayoutChange({ col: currentPosRef.current.col, row: currentPosRef.current.row });
    };

    const handleToggleCollapse = (e) => {
        e.stopPropagation();

        // isCollapsed - це поточний стан, після toggleCollapse він зміниться на протилежний
        const willBeCollapsed = !isCollapsed;
        if (willBeCollapsed && layout.rows > 2) {
            setExpandedRow(blockId, layout.rows);
        }
        toggleCollapse(blockId);

        // Оновлюємо висоту в layout
        if (willBeCollapsed) {
            // Згортаємо - встановлюємо 2 рядки
            onLayoutChange({ rows: 2 });
        } else {
            // Розгортаємо - повертаємо збережену висоту
            const restoredRows = expandedRows?.[blockId] || originalRowsRef.current;
            onLayoutChange({ rows: restoredRows || 12 });
        }
    };

    const gridStyle = {
        gridColumn: `${layout.col} / span ${layout.cols}`,
        gridRow: `${layout.row} / span ${layout.rows}`,
        visibility: isVisible ? 'visible' : 'hidden',
        position: isVisible ? 'relative' : 'absolute',
        pointerEvents: isVisible ? 'auto' : 'none'
    };

    return (
        <Paper
            ref={blockRef}
            shadow="sm"
            radius="md"
            withBorder
            className="grid-block"
            style={gridStyle}
        >
            <Title
                order={6}
                mb="md"
                className="grid-block-title"
                onMouseDown={handleDragStart}
                style={{ cursor: isEditMode ? 'move' : 'default', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}
            >
                <div style={{ display: 'flex', alignItems: 'center' }}>
                    <Icon size={20} style={{ verticalAlign: 'middle', marginRight: 8 }} />
                    {title}
                </div>
                <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                    {headerActions && <div style={{ display: 'flex', gap: '4px', marginRight: '4px' }}>{headerActions}</div>}
                    <ActionIcon
                        size="sm"
                        variant="subtle"
                        color="gray"
                        onClick={handleToggleCollapse}
                        style={{ cursor: 'pointer' }}
                        title={isCollapsed ? "Розгорнути" : "Згорнути"}
                    >
                        {isCollapsed ? <IconChevronDown size={16} /> : <IconChevronUp size={16} />}
                    </ActionIcon>
                    {isEditMode && onHide && (
                        <ActionIcon
                            size="sm"
                            variant="subtle"
                            color="gray"
                            onClick={(e) => {
                                e.stopPropagation();
                                onHide(blockId);
                            }}
                            style={{ cursor: 'pointer' }}
                            title="Видалити"
                        >
                            <IconX size={16} />
                        </ActionIcon>
                    )}
                </div>
            </Title>
            <div
                className="grid-block-content"
                style={{
                    display: isCollapsed ? 'none' : 'flex',
                    flexDirection: 'column',
                    flex: 1
                }}
            >
                {children}
            </div>
            {!isCollapsed && isEditMode && (
                <div
                    className="resize-handle"
                    onMouseDown={handleResizeStart}
                />
            )}
        </Paper>
    );
}

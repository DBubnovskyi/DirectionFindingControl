import React, { useRef } from 'react';
import { Paper, Title, ActionIcon } from '@mantine/core';
import { IconX } from '@tabler/icons-react';
import './GridBlock.css';

export default function GridItem({ blockId, title, icon: Icon, children, layout, onLayoutChange, onDragPreview, onDragEnd, onHide }) {

    // Захист від undefined layout
    if (!layout) {
        return null;
    }

    const resizingRef = useRef(false);
    const draggingRef = useRef(false);
    const startPosRef = useRef({ x: 0, y: 0 });
    const startSizeRef = useRef({ cols: 0, rows: 0 });
    const startGridPosRef = useRef({ col: 0, row: 0 });
    const currentSizeRef = useRef({ cols: layout.cols, rows: layout.rows });
    const currentPosRef = useRef({ col: layout.col, row: layout.row });
    const blockRef = useRef(null);

    const handleResizeStart = (e) => {
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
        const colWidth = 16;
        const rowHeight = 16 + gridGap;

        const deltaCols = Math.round(deltaX / colWidth);
        const deltaRows = Math.round(deltaY / rowHeight);

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
    };

    const handleDragStart = (e) => {
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

        const gridGap = 10;
        const colWidth = 16;
        const rowHeight = 16 + gridGap;

        const deltaCols = Math.round(deltaX / colWidth);
        const deltaRows = Math.round(deltaY / rowHeight);

        const newCol = Math.max(1, Math.min(97 - layout.cols, startGridPosRef.current.col + deltaCols));
        const newRow = Math.max(1, startGridPosRef.current.row + deltaRows);

        currentPosRef.current = { col: newCol, row: newRow };

        if (blockRef.current) {
            // blockRef.current.style.gridColumn = `${newCol} / span ${layout.cols}`;
            // blockRef.current.style.gridRow = `${newRow} / span ${layout.rows}`;
            onLayoutChange({ col: newCol, row: newRow });
        }

        // Показуємо placeholder де буде блок після компактування
        if (onDragPreview) {
            onDragPreview(currentPosRef.current);
        }
    };

    const handleDragEndHandler = () => {
        draggingRef.current = false;
        document.removeEventListener('mousemove', handleDragMove);
        document.removeEventListener('mouseup', handleDragEndHandler);

        if (blockRef.current) {
            blockRef.current.style.opacity = '1';
            blockRef.current.style.zIndex = 'auto';
        }

        if (onDragEnd) {
            onDragEnd();
        }

        onLayoutChange({ col: currentPosRef.current.col, row: currentPosRef.current.row });
    };

    const gridStyle = {
        gridColumn: `${layout.col} / span ${layout.cols}`,
        gridRow: `${layout.row} / span ${layout.rows}`
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
                style={{ cursor: 'move', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}
            >
                <div style={{ display: 'flex', alignItems: 'center' }}>
                    <Icon size={20} style={{ verticalAlign: 'middle', marginRight: 8 }} />
                    {title}
                </div>
                {onHide && (
                    <ActionIcon
                        size="sm"
                        variant="subtle"
                        color="gray"
                        onClick={(e) => {
                            e.stopPropagation();
                            onHide(blockId);
                        }}
                        style={{ cursor: 'pointer' }}
                    >
                        <IconX size={16} />
                    </ActionIcon>
                )}
            </Title>
            <div className="grid-block-content">
                {children}
            </div>
            <div
                className="resize-handle"
                onMouseDown={handleResizeStart}
            />
        </Paper>
    );
}

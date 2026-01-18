import React, { useState, useEffect, Children, cloneElement } from 'react';
import GridItem from './GridItem';
import './GridBlock.css';

const INITIAL_LAYOUTS = {
    0: { col: 1, row: 1, cols: 6, rows: 12 },
    1: { col: 7, row: 1, cols: 6, rows: 12 },
    2: { col: 13, row: 1, cols: 12, rows: 30 }
};

const STORAGE_KEY = 'gridBlockLayout';

export default function GridBlock({ children, onHideBlock, onLayoutChange, layoutToLoad, onLayoutLoaded }) {
    const childrenArray = Children.toArray(children);

    const [layout, setLayout] = useState(() => {
        // Спробувати завантажити з localStorage
        try {
            const saved = localStorage.getItem(STORAGE_KEY);
            if (saved) {
                const parsedLayout = JSON.parse(saved);
                // Перевірити що всі блоки є в збереженому layout
                const hasAllBlocks = childrenArray.every((_, index) => parsedLayout[index]);
                if (hasAllBlocks) {
                    return parsedLayout;
                }
            }
        } catch (error) {
            console.error('Error loading layout from localStorage:', error);
        }

        // Якщо не вдалось завантажити - використати дефолтний
        const initialLayout = {};
        childrenArray.forEach((child, index) => {
            initialLayout[index] = INITIAL_LAYOUTS[index] || { col: 1, row: 1, cols: 6, rows: 12 };
        });
        return initialLayout;
    });

    // Зберігати layout в localStorage при кожній зміні
    useEffect(() => {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(layout));
        } catch (error) {
            console.error('Error saving layout to localStorage:', error);
        }
        // Передати layout в App для збереження
        if (onLayoutChange) {
            onLayoutChange(layout);
        }
    }, [layout, onLayoutChange]);

    // Завантажити layout з пропсів якщо передано
    useEffect(() => {
        if (layoutToLoad) {
            setLayout(layoutToLoad);
            if (onLayoutLoaded) {
                onLayoutLoaded();
            }
        }
    }, [layoutToLoad, onLayoutLoaded]);

    const [dragPlaceholder, setDragPlaceholder] = useState(null);

    // Функція вертикального компактування
    const compactLayout = (newLayout) => {
        const blocks = Object.keys(newLayout).map(id => ({ id, ...newLayout[id] }));

        blocks.sort((a, b) => {
            if (a.row !== b.row) return a.row - b.row;
            return a.col - b.col;
        });

        const compacted = {};
        const occupied = [];

        blocks.forEach(block => {
            let targetRow = 1;
            let canPlace = false;

            while (!canPlace) {
                canPlace = true;

                for (let checkRow = targetRow; checkRow < targetRow + block.rows; checkRow++) {
                    for (let checkCol = block.col; checkCol < block.col + block.cols; checkCol++) {
                        const key = `${checkRow}-${checkCol}`;
                        if (occupied.includes(key)) {
                            canPlace = false;
                            targetRow++;
                            break;
                        }
                    }
                    if (!canPlace) break;
                }
            }

            for (let r = targetRow; r < targetRow + block.rows; r++) {
                for (let c = block.col; c < block.col + block.cols; c++) {
                    occupied.push(`${r}-${c}`);
                }
            }

            compacted[block.id] = {
                col: block.col,
                row: targetRow,
                cols: block.cols,
                rows: block.rows
            };
        });

        return compacted;
    };

    const updateBlockLayout = (blockId, updates) => {
        const newLayout = {
            ...layout,
            [blockId]: { ...layout[blockId], ...updates }
        };
        const compactedLayout = compactLayout(newLayout);
        setLayout(compactedLayout);
    };

    const previewBlockPosition = (blockId, updates) => {
        const tempLayout = {
            ...layout,
            [blockId]: { ...layout[blockId], ...updates }
        };
        const compactedLayout = compactLayout(tempLayout);
        setDragPlaceholder(compactedLayout[blockId]);
    };

    const clearPlaceholder = () => {
        setDragPlaceholder(null);
    };

    return (
        <div className="App">
            {dragPlaceholder && (
                <div
                    className="drag-placeholder"
                    style={{
                        gridColumn: `${dragPlaceholder.col} / span ${dragPlaceholder.cols}`,
                        gridRow: `${dragPlaceholder.row} / span ${dragPlaceholder.rows}`
                    }}
                />
            )}
            {childrenArray.map((child, index) => {
                const blockId = child.props.blockId !== undefined ? child.props.blockId : index;
                return (
                    <GridItem
                        key={child.key || index}
                        blockId={blockId}
                        title={child.props.title}
                        icon={child.props.icon}
                        layout={layout[index]}
                        onLayoutChange={(updates) => updateBlockLayout(index, updates)}
                        onDragPreview={(updates) => previewBlockPosition(index, updates)}
                        onDragEnd={clearPlaceholder}
                        onHide={onHideBlock}
                    >
                        {child}
                    </GridItem>
                );
            })}
        </div>
    );
}

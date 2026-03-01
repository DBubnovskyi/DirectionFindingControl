import React, { useState, useEffect, Children, useMemo } from 'react';
import GridItem from './GridItem';
import './GridBlock.scss';
import { useApp } from '../../contexts/AppContext';

const STORAGE_KEY = 'gridBlockLayout';

export default function GridBlock({ children }) {
    const { blocks, visibleBlocks, layoutToLoad, setCurrentLayout, hideBlock, registerBlocks } = useApp();
    const childrenArray = useMemo(() => Children.toArray(children), [children]);

    // Реєструємо блоки в контексті при монтуванні
    useEffect(() => {
        const blocksData = childrenArray.map((child, index) => ({
            id: index,
            title: child.props.title,
            icon: child.props.icon,
            defaultSize: child.props.defaultSize
        }));
        registerBlocks(blocksData);
    }, [childrenArray.length, registerBlocks]);

    // Функція для знаходження вільного місця для нового блоку
    const findFreePosition = (existingLayout, cols = 6, rows = 12) => {
        const occupied = [];

        // Заповнити масив зайнятих клітинок
        Object.values(existingLayout).forEach(block => {
            for (let r = block.row; r < block.row + block.rows; r++) {
                for (let c = block.col; c < block.col + block.cols; c++) {
                    occupied.push(`${r}-${c}`);
                }
            }
        });

        // Спробувати розмістити блок починаючи з позиції (1, 1)
        for (let col = 1; col <= 97 - cols; col++) {
            for (let row = 1; row <= 100; row++) {
                let canPlace = true;

                // Перевірити чи всі клітинки вільні
                for (let checkRow = row; checkRow < row + rows; checkRow++) {
                    for (let checkCol = col; checkCol < col + cols; checkCol++) {
                        const key = `${checkRow}-${checkCol}`;
                        if (occupied.includes(key)) {
                            canPlace = false;
                            break;
                        }
                    }
                    if (!canPlace) break;
                }

                if (canPlace) {
                    return { col, row, cols, rows };
                }
            }
        }

        // Якщо не знайшли вільного місця - повернути дефолтну позицію внизу
        const maxRow = Math.max(...Object.values(existingLayout).map(b => b.row + b.rows - 1), 0);
        return { col: 1, row: maxRow + 1, cols, rows };
    };

    const [layout, setLayout] = useState(() => {
        // Спробувати завантажити з localStorage
        try {
            const saved = localStorage.getItem(STORAGE_KEY);
            if (saved) {
                return JSON.parse(saved);
            }
        } catch (error) {
            console.error('Error loading layout from localStorage:', error);
        }

        // Якщо не вдалось завантажити - згенерувати початковий layout автоматично
        const initialLayout = {};
        childrenArray.forEach((child, index) => {
            const defaultSize = child.props.defaultSize || { cols: 6, rows: 12 };
            initialLayout[index] = findFreePosition(initialLayout, defaultSize.cols, defaultSize.rows);
        });
        return initialLayout;
    });

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

    // Оновлювати layout коли змінюються видимі блоки
    useEffect(() => {
        const layoutBlockIds = Object.keys(layout).map(Number);

        // Знайти blockId які є в layout але не видимі (приховані блоки)
        const hiddenBlockIds = layoutBlockIds.filter(id => !visibleBlocks.includes(id));

        // Знайти blockId які видимі але яких немає в layout (ново додані блоки)
        const newBlockIds = visibleBlocks.filter(id => !layoutBlockIds.includes(id));

        if (hiddenBlockIds.length > 0 || newBlockIds.length > 0) {
            setLayout(prevLayout => {
                let newLayout = { ...prevLayout };

                // Видалити з layout приховані блоки
                hiddenBlockIds.forEach(id => {
                    console.log('Removing block', id, 'from layout');
                    delete newLayout[id];
                });

                // Якщо були видалені блоки - виконати компактування
                if (hiddenBlockIds.length > 0) {
                    newLayout = compactLayout(newLayout);
                }

                // Додати в layout нові блоки з defaultSize
                newBlockIds.forEach(id => {
                    const defaultSize = blocks[id]?.defaultSize || { cols: 6, rows: 12 };
                    console.log('Adding block', id, 'with defaultSize:', defaultSize, 'from blocks:', blocks[id]);
                    newLayout[id] = findFreePosition(newLayout, defaultSize.cols, defaultSize.rows);
                });

                return newLayout;
            });
        }
    }, [visibleBlocks, blocks]);

    // Зберігати layout в localStorage при кожній зміні
    useEffect(() => {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(layout));
        } catch (error) {
            console.error('Error saving layout to localStorage:', error);
        }
        setCurrentLayout(layout);
    }, [layout, setCurrentLayout]);

    // Завантажити layout з пропсів якщо передано
    useEffect(() => {
        if (layoutToLoad) {
            setLayout(layoutToLoad);
        }
    }, [layoutToLoad]);

    const [dragPlaceholder, setDragPlaceholder] = useState(null);

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
                const blockId = index;
                const isVisible = visibleBlocks.includes(blockId);
                const title = child.props.title;
                const icon = child.props.icon;
                const headerActions = child.type?.headerActions;

                // Клонуємо child щоб зберегти той самий екземпляр React елемента
                const clonedChild = React.cloneElement(child, { key: `child-${blockId}` });

                return (
                    <GridItem
                        key={blockId}
                        blockId={blockId}
                        title={title}
                        icon={icon}
                        layout={layout[blockId]}
                        onLayoutChange={(updates) => updateBlockLayout(blockId, updates)}
                        onDragPreview={(updates) => previewBlockPosition(blockId, updates)}
                        onDragEnd={clearPlaceholder}
                        onHide={hideBlock}
                        isVisible={isVisible}
                        headerActions={headerActions}
                    >
                        {clonedChild}
                    </GridItem>
                );
            })}
        </div>
    );
}

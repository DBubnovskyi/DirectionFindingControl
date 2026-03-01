import React, { useState } from 'react';
import { Group, Title, Text, Menu, Button, Modal, TextInput, Stack, Checkbox } from '@mantine/core';
import { IconRadar, IconLayoutGridAdd, IconDeviceFloppy, IconTrash, IconLockOpen2, IconLock, IconPencil } from '@tabler/icons-react';
import './Header.scss';
import { useApp } from '../../contexts/AppContext';

export default function Header() {
    const {
        hiddenBlocks,
        showBlock,
        savedLayouts,
        saveLayout,
        loadLayout,
        deleteLayout,
        updateLayout,
        currentLayout,
        visibleBlocks,
        collapsedBlocks,
        expandedRows,
        activeLayoutId,
        isEditMode,
        toggleEditMode
    } = useApp();
    const [saveModalOpened, setSaveModalOpened] = useState(false);
    const [layoutName, setLayoutName] = useState('');
    const [editModalOpened, setEditModalOpened] = useState(false);
    const [editLayoutId, setEditLayoutId] = useState(null);
    const [editLayoutName, setEditLayoutName] = useState('');
    const [applyCurrentLayout, setApplyCurrentLayout] = useState(true);

    const handleSaveLayout = () => {
        if (layoutName.trim() && currentLayout) {
            saveLayout(layoutName.trim());
            setLayoutName('');
            setSaveModalOpened(false);
        }
    };

    const openEditLayout = (layout) => {
        setEditLayoutId(layout.id);
        setEditLayoutName(layout.name || '');
        setApplyCurrentLayout(true);
        setEditModalOpened(true);
    };

    const handleEditLayout = () => {
        if (!editLayoutId || !editLayoutName.trim()) return;

        const updates = {
            name: editLayoutName.trim(),
        };

        if (applyCurrentLayout && currentLayout) {
            updates.gridLayout = { ...currentLayout };
            updates.visibleBlocks = [...visibleBlocks];
            updates.collapsedBlocks = [...collapsedBlocks];
            updates.expandedRows = { ...expandedRows };
        }

        updateLayout(editLayoutId, updates);
        setEditModalOpened(false);
        setEditLayoutId(null);
        setEditLayoutName('');
    };

    return (
        <>
            <header className="app-header">
                <Group justify="space-between" h="100%">
                    <Group gap="md">
                        <IconRadar size={32} stroke={1.5} />
                        <div>
                            <Title order={3} className="app-title">
                                Direction Finding Control
                            </Title>
                            <Text size="sm" c="dimmed" className="app-subtitle">
                                <a href="/main">Система керування пеленгатором</a>
                            </Text>
                        </div>
                    </Group>
                    <Group gap="md">
                        {/* Приховані блоки */}
                        {isEditMode && hiddenBlocks.length > 0 && (
                            <Menu shadow="md" width={200}>
                                <Menu.Target>
                                    <Button
                                        variant="light"
                                        size="sm"
                                        leftSection={<IconLayoutGridAdd size={16} />}
                                    >
                                        Блоки ({hiddenBlocks.length})
                                    </Button>
                                </Menu.Target>
                                <Menu.Dropdown>
                                    <Menu.Label>Додати блок</Menu.Label>
                                    {hiddenBlocks.map(block => (
                                        <Menu.Item
                                            key={block.id}
                                            leftSection={<block.icon size={16} />}
                                            onClick={() => showBlock(block.id)}
                                        >
                                            {block.title}
                                        </Menu.Item>
                                    ))}
                                </Menu.Dropdown>
                            </Menu>
                        )}
                        {/* Меню лейаутів */}
                        <Menu shadow="md" width={250}>
                            <Menu.Target>
                                <Button
                                    variant="light"
                                    size="sm"
                                    leftSection={<IconDeviceFloppy size={16} />}
                                >
                                    Компонування ({savedLayouts.length})
                                </Button>
                            </Menu.Target>
                            <Menu.Dropdown>
                                <Menu.Label>Управління компонуванням</Menu.Label>
                                <Menu.Item
                                    leftSection={<IconDeviceFloppy size={16} />}
                                    onClick={() => setSaveModalOpened(true)}
                                    disabled={!currentLayout}
                                >
                                    Зберегти поточний
                                </Menu.Item>
                                <Menu.Item
                                    leftSection={isEditMode ? <IconLockOpen2 size={16} /> : <IconLock size={16} />}
                                    onClick={toggleEditMode}
                                >
                                    редагування
                                </Menu.Item>
                                {savedLayouts.length > 0 && (
                                    <>
                                        <Menu.Divider />
                                        <Menu.Label>Збережені лейаути</Menu.Label>
                                        {savedLayouts.map(layout => (
                                            <Menu.Item
                                                key={layout.id}
                                                onClick={() => loadLayout(layout.id)}
                                                color={layout.id === activeLayoutId ? 'blue' : 'white'}
                                                bg={layout.id === activeLayoutId ? '#444' : 'transparent'}
                                                style={{
                                                    fontWeight: layout.id === activeLayoutId ? 600 : 400
                                                }}
                                                rightSection={
                                                    <Group gap={6}>
                                                        <IconPencil
                                                            size={14}
                                                            onClick={(e) => {
                                                                e.stopPropagation();
                                                                openEditLayout(layout);
                                                            }}
                                                            style={{ cursor: 'pointer', color: 'var(--mantine-color-blue-6)' }}
                                                        />
                                                        <IconTrash
                                                            size={14}
                                                            onClick={(e) => {
                                                                e.stopPropagation();
                                                                deleteLayout(layout.id);
                                                            }}
                                                            style={{ cursor: 'pointer', color: 'var(--mantine-color-red-6)' }}
                                                        />
                                                    </Group>
                                                }
                                            >
                                                {layout.name}
                                            </Menu.Item>
                                        ))}
                                    </>
                                )}
                            </Menu.Dropdown>
                        </Menu>
                        {/* Режим редагування */}
                        <Text size="sm" c="dimmed">
                            v1.0.0
                        </Text>
                    </Group>
                </Group>
            </header>

            {/* Модалка збереження лейауту */}
            <Modal
                opened={saveModalOpened}
                onClose={() => {
                    setSaveModalOpened(false);
                    setLayoutName('');
                }}
                title="Зберегти лейаут"
                size="sm"
            >
                <Stack>
                    <TextInput
                        label="Назва лейауту"
                        placeholder="Введіть назву..."
                        value={layoutName}
                        onChange={(e) => setLayoutName(e.target.value)}
                        onKeyPress={(e) => {
                            if (e.key === 'Enter') {
                                handleSaveLayout();
                            }
                        }}
                        data-autofocus
                    />
                    <Button onClick={handleSaveLayout} disabled={!layoutName.trim()}>
                        Зберегти
                    </Button>
                </Stack>
            </Modal>

            {/* Модалка редагування лейауту */}
            <Modal
                opened={editModalOpened}
                onClose={() => {
                    setEditModalOpened(false);
                    setEditLayoutId(null);
                    setEditLayoutName('');
                }}
                title="Редагувати лейаут"
                size="sm"
            >
                <Stack>
                    <TextInput
                        label="Назва лейауту"
                        placeholder="Введіть назву..."
                        value={editLayoutName}
                        onChange={(e) => setEditLayoutName(e.target.value)}
                        onKeyPress={(e) => {
                            if (e.key === 'Enter') {
                                handleEditLayout();
                            }
                        }}
                        data-autofocus
                    />
                    <Checkbox
                        label="Оновити поточним компонуванням"
                        checked={applyCurrentLayout}
                        onChange={(e) => setApplyCurrentLayout(e.currentTarget.checked)}
                        disabled={!currentLayout}
                    />
                    <Button onClick={handleEditLayout} disabled={!editLayoutName.trim()}>
                        Застосувати
                    </Button>
                </Stack>
            </Modal>
        </>
    );
}

import React, { useState } from 'react';
import { Group, Title, Text, Menu, Button, Modal, TextInput, Stack } from '@mantine/core';
import { IconRadar, IconLayoutGridAdd, IconDeviceFloppy, IconTrash } from '@tabler/icons-react';
import './Header.css';

export default function Header({
    hiddenBlocks = [],
    onShowBlock,
    savedLayouts = [],
    onSaveLayout,
    onLoadLayout,
    onDeleteLayout,
    currentLayout,
    activeLayoutId
}) {
    const [saveModalOpened, setSaveModalOpened] = useState(false);
    const [layoutName, setLayoutName] = useState('');

    const handleSaveLayout = () => {
        if (layoutName.trim() && currentLayout) {
            onSaveLayout(layoutName.trim());
            setLayoutName('');
            setSaveModalOpened(false);
        }
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
                                Система керування пеленгатором
                            </Text>
                        </div>
                    </Group>
                    <Group gap="md">
                        {/* Приховані блоки */}
                        {hiddenBlocks.length > 0 && (
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
                                            onClick={() => onShowBlock(block.id)}
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
                                    Лейаути ({savedLayouts.length})
                                </Button>
                            </Menu.Target>
                            <Menu.Dropdown>
                                <Menu.Label>Управління лейаутами</Menu.Label>
                                <Menu.Item
                                    leftSection={<IconDeviceFloppy size={16} />}
                                    onClick={() => setSaveModalOpened(true)}
                                    disabled={!currentLayout}
                                >
                                    Зберегти поточний
                                </Menu.Item>
                                {savedLayouts.length > 0 && (
                                    <>
                                        <Menu.Divider />
                                        <Menu.Label>Збережені лейаути</Menu.Label>
                                        {savedLayouts.map(layout => (
                                            <Menu.Item
                                                key={layout.id}
                                                onClick={() => onLoadLayout(layout.id)}
                                                color={layout.id === activeLayoutId ? 'blue' : 'white'}
                                                bg={layout.id === activeLayoutId ? '#444' : 'transparent'}
                                                style={{
                                                    fontWeight: layout.id === activeLayoutId ? 600 : 400
                                                }}
                                                rightSection={
                                                    <IconTrash
                                                        size={14}
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            onDeleteLayout(layout.id);
                                                        }}
                                                        style={{ cursor: 'pointer', color: 'var(--mantine-color-red-6)' }}
                                                    />
                                                }
                                            >
                                                {layout.name}
                                            </Menu.Item>
                                        ))}
                                    </>
                                )}
                            </Menu.Dropdown>
                        </Menu>
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
        </>
    );
}

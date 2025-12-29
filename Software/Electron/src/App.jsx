import React from 'react';
import { Title, Text, Button, Card } from '@mantine/core';
import './App.css';
import LeftPanel from './components/LeftPanel/LeftPanel';
import RightPanel from './components/RightPanel/RightPanel';

function App() {
    return (
        <div className="App">
            <LeftPanel>
                <Title order={2}>Лівий контейнер</Title>
                <Text>Тут можна розмістити компоненти</Text>
                <Button variant="filled" mt="md">Кнопка</Button>
                
                <Card shadow="sm" padding="lg" radius="md" withBorder mt="md">
                    <Title order={4}>Картка</Title>
                    <Text size="sm" c="dimmed" mt="xs">
                        Інформаційна картка з даними
                    </Text>
                    <Button variant="light" color="blue" fullWidth mt="md">
                        Дія
                    </Button>
                </Card>
            </LeftPanel>
            <RightPanel>
                <Title order={2}>Правий контейнер</Title>
                <Text>Тут можна розмістити компоненти</Text>
            </RightPanel>
        </div>
    );
}

export default App;

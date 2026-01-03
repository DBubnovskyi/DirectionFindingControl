import React from 'react';
import './RightPanel.css';
import { Title, Text, Button, Card } from '@mantine/core';

export default function RightPanel() {
  return (
    <div className="right-panel">
      <Title order={2}>Правий контейнер</Title>
      <Text>Тут можна розмістити компоненти</Text>
    </div>
  );
}

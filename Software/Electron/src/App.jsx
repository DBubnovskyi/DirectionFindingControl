import React from 'react';
import { Title, Text, Button, Card } from '@mantine/core';
import './App.css';
import LeftPanel from './components/LeftPanel/LeftPanel';
import RightPanel from './components/RightPanel/RightPanel';

export default function App() {
    return (
        <div className="App">
            <LeftPanel />
            <RightPanel />
        </div>
    );
}
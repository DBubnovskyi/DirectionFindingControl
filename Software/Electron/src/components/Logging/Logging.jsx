import React, { useEffect, useRef } from 'react';
import { ScrollArea, ActionIcon } from '@mantine/core';
import { IconTrash } from '@tabler/icons-react';
import { useLogging } from '../../contexts/LoggingContext';

function LoggingHeaderActions() {
    const { clearLogs } = useLogging();

    return (
        <ActionIcon
            size="sm"
            variant="subtle"
            color="gray"
            title="Очистити логи"
            onClick={clearLogs}
        >
            <IconTrash size={16} />
        </ActionIcon>
    );
}

export default function Logging() {
    const { logs } = useLogging();
    const scrollRef = useRef(null);

    // Автоматичне прокручування до низу при додаванні нових логів
    useEffect(() => {
        if (scrollRef.current) {
            scrollRef.current.scrollTo({ top: scrollRef.current.scrollHeight });
        }
    }, [logs]);

    return (
        <div style={{ display: 'flex', flexDirection: 'column', height: '100%', gap: '8px' }}>
            <ScrollArea
                ref={scrollRef}
                style={{
                    flex: 1,
                    fontFamily: 'monospace',
                    fontSize: '12px',
                    padding: '8px',
                    backgroundColor: 'var(--mantine-color-dark-8)',
                    borderRadius: '4px'
                }}
            >
                {logs.length === 0 ? (
                    <div style={{ color: 'var(--mantine-color-dimmed)' }}>
                        Логи відсутні...
                    </div>
                ) : (
                    logs.map(log => (
                        <div
                            key={log.id}
                            style={{
                                color: log.direction === 'TX' ? '#4dabf7' : '#51cf66',
                                marginBottom: '2px'
                            }}
                        >
                            {log.timestamp} {log.direction}: {log.message}
                        </div>
                    ))
                )}
            </ScrollArea>
        </div>
    );
}

Logging.headerActions = <LoggingHeaderActions />;

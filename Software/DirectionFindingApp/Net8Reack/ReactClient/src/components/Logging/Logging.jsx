import React, { useEffect, useRef } from 'react';
import { ScrollArea, ActionIcon } from '@mantine/core';
import { IconTrash } from '@tabler/icons-react';
import { useApp } from '../../contexts/AppContext';
import './Logging.scss';

function LoggingHeaderActions() {
    const { clearLogs } = useApp();

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
    const { logs } = useApp();
    const scrollRef = useRef(null);

    // Автоматичне прокручування до низу при додаванні нових логів
    useEffect(() => {
        if (scrollRef.current) {
            scrollRef.current.scrollTo({ top: scrollRef.current.scrollHeight });
        }
    }, [logs]);
    console.log(logs);

    return (
        <div className="logging-container">
            <ScrollArea
                ref={scrollRef}
                className="logging-scroll"
            >
                {logs?.length === 0 ? (
                    <div className="logging-empty">
                        Логи відсутні...
                    </div>
                ) : (
                    logs.map(log => (
                        <p className={`logging-line`}>
                            {log}
                        </p>
                    ))
                )}
            </ScrollArea>
        </div>
    );
}

Logging.headerActions = <LoggingHeaderActions />;

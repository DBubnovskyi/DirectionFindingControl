import React from 'react';
import './LeftPanel.css';
import { Accordion, Container, Title } from '@mantine/core';
import { IconPlugConnected, IconAdjustmentsDown, IconCompass } from '@tabler/icons-react';
import SerialPortSelector from '../SerialPortSelector/SerialPortSelector';
import SerialControl from '../SerialControl/SerialControl';

const placeholder =
  `It can’t help but hear a pin drop from over half a mile away, so it lives deep in the mountains where there aren’t 
  many people or Pokémon.It was born from sludge on the ocean floor.In a sterile environment, the germs within its body
  can’t multiply, and it dies.It has no eyeballs, so it can’t see.It checks its surroundings via the ultrasonic waves
  it emits from its mouth.`;

export default function LeftPanel() {
  return (
    <div className="left-panel">
      <Container size="sm" className="wrapper" style={{ padding: 0 }}>
        <Title className="title" style={{ paddingBottom: 14, fontSize: 24 }}>
          Керування поворотним механізмом
        </Title>

        <Accordion variant="separated" multiple defaultValue={['reset-password', 'newsletter']} size="xs">
          <Accordion.Item value="reset-password">
            <Accordion.Control icon={<IconPlugConnected size={20} />}>Підключення</Accordion.Control>
            <Accordion.Panel>
              <SerialPortSelector />
            </Accordion.Panel>
          </Accordion.Item>

          <Accordion.Item value="another-account">
            <Accordion.Control icon={<IconAdjustmentsDown size={20} />}>Ініціалізація</Accordion.Control>
            <Accordion.Panel>{placeholder}</Accordion.Panel>
          </Accordion.Item>

          <Accordion.Item value="newsletter">
            <Accordion.Control icon={<IconCompass size={20} />}>Керування</Accordion.Control>
            <Accordion.Panel>
              <SerialControl />
            </Accordion.Panel>
          </Accordion.Item>
        </Accordion>
      </Container>
    </div>
  );
}

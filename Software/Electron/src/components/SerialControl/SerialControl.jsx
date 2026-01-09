
import React, { useState } from 'react';
import { rotatorState } from '../../types/rotatorState';
import './SerialControl.css';
import ScaleAz from '../SVG/ScaleAz';
import ScaleAn from '../SVG/ScaleAn';
import DataCircle from '../SVG/DataCircle';
import TextAn from '../SVG/TextAn';
import TextAz from '../SVG/TextAz';
import SetPointer from '../SVG/SetPointer';
import RealPointer from '../SVG/RealPointer';

export default function SerialControl() {
    const [angles, setAngles] = useState(rotatorState);
    return (
        <div className="serial-control">
            <div className="compass_container">
                <ScaleAz />
                <ScaleAn />
                <DataCircle />
                <TextAn />
                <TextAz />
                <SetPointer angle={angles.targetAzimuth} />
                <RealPointer angle={angles.currentAzimuth} />
            </div>
        </div>
    );
}

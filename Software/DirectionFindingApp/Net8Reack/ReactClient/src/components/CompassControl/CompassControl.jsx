
import React from 'react';
import './CompassControl.scss';
import ScaleAz from '../SVG/ScaleAz';
import ScaleAn from '../SVG/ScaleAn';
import DataCircle from '../SVG/DataCircle';
import TextAn from '../SVG/TextAn';
import TextAz from '../SVG/TextAz';
import SetPointer from '../SVG/SetPointer';
import RealPointer from '../SVG/RealPointer';
import { useApp } from '../../contexts/AppContext';

export default function CompassControl() {
    const { angles } = useApp();

    return (
        <div className="compass-control">
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

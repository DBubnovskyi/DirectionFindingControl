import React from 'react';
import './SerialControl.css';
import ScaleAz from '../SVG/ScaleAz';
import ScaleAn from '../SVG/ScaleAn';

export default function SerialControl() {
    return (
        <div className="serial-control">
            <div className="compass_container">
                <ScaleAz />
                <ScaleAn />
            </div>
        </div>
    );
}

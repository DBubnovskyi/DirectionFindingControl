import React from 'react';

const svgTop = { fill: '#76D9EB', stroke: '#438e9b', strokeWidth: 2 };
const svgBottom = { fill: '#e0dd46', stroke: '#868532', strokeWidth: 2 };
const textTop = { fontFamily: 'monospace', dominantBaseline: 'middle', textAnchor: 'middle', fill: '#438e9b', strokeWidth: 0, fontSize: '1rem', fontWeight: 'bold' };
const valueTop = { fontFamily: 'monospace', dominantBaseline: 'middle', textAnchor: 'middle', fill: '#438e9b', strokeWidth: 0, fontSize: '3rem', fontWeight: 'bold' };
const textBottom = { fontFamily: 'monospace', dominantBaseline: 'middle', textAnchor: 'middle', fill: '#868532', strokeWidth: 0, fontSize: '1rem', fontWeight: 'bold' };
const valueBottom = { fontFamily: 'monospace', dominantBaseline: 'middle', textAnchor: 'middle', fill: '#868532', strokeWidth: 0, fontSize: '3rem', fontWeight: 'bold' };

export default function DataCircle() {
    return (
        <svg className="scale_3" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 400">
            <defs>
                <mask id="circlBottom">
                    <rect width="600" height="600" fill="white" />
                    <rect width="600" height="200" fill="black" />
                </mask>
                <mask id="circlTop">
                    <rect width="600" height="600" fill="white" />
                    <rect y="200" width="600" height="150" fill="black" />
                </mask>
            </defs>
            <circle cx="200" cy="200" r="100" style={svgTop} mask="url(#circlTop)" />
            <circle cx="200" cy="200" r="100" style={svgBottom} mask="url(#circlBottom)" />
            <text style={valueTop} key="current" x="200" y="160">---.--</text>
            <text style={textTop} x="200" y="190">Поточний азимут</text>
            <text style={textBottom} x="200" y="213">Заданий азимут</text>
            <text style={valueBottom} key="set" x="200" y="250">---</text>
        </svg>
    )
}
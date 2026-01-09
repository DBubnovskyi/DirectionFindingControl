import React from 'react';

export default function TextAn({ text = 'кут' }) {
    return (
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 500 500"
            style={style1}>
            <g transform="translate(50, 50)">
                <path style={style2} d=" M 160,-14 A 5,2.6 0 0,1 240,-14 A 13,1.5 0 0,0 160,-14" />
                <text x="200" y="-28" style={style3}>{text}</text>
            </g>
        </svg>
    )
}

const style1 = { pointerEvents: 'none', position: 'absolute', top: 0, left: 0, width: '100%', height: '100%' };
const style2 = { fill: '#333', strokeWidth: 1 };
const style3 = { fontFamily: 'monospace', fontSize: '12px', fill: '#ccc', textAnchor: 'middle', dominantBaseline: 'middle' };

import React from 'react';

export default function TextAz({ text = 'азимут' }) {
    return (
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 500 500"
            style={style1}>
            <g transform="translate(50, 50)">
                <path style={style2} d=" M 160,77 A 6,1.7 0 0,0 240,77 A 3,0.6 0 0,0 160,77" />
                <text x="200" y="80" style={style3} >{text}</text>
            </g>
        </svg>
    )
}

const style1 = { pointerEvents: 'none', position: 'absolute', top: 0, left: 0, width: '100%', height: '100%' };
const style2 = { fill: '#333', strokeWidth: 1 };
const style3 = { fontFamily: 'monospace', fontSize: '12px', fill: '#ccc', textAnchor: 'middle', dominantBaseline: 'middle' };

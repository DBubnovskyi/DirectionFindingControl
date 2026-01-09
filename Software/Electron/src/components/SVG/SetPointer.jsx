import React from 'react';

export default function SetPointer({ angle = 0 }) {
    const style0 = { ...style1, transform: `rotate(${angle}deg)` };
    return (
        <svg xmlns="http://www.w3.org/2000/svg" width="600" height="600" viewBox="0 0 600 600" style={style0}>
            <g><polygon style={style2} points="300,110 290,128 310,128" /></g>
        </svg>
    )
}

const style1 = {
    pointerEvents: 'none',
    position: 'absolute',
    width: '92%',
    height: '92%'
};

const style2 = {
    fill: '#e0dd46',
    stroke: '#868532',
    strokeWidth: 2
};
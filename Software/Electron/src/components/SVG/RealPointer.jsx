import React from 'react';

export default function RealPointer({ angle = 0 }) {
    const style0 = { ...style1, transform: `rotate(${angle}deg)` };
    return (
        <svg xmlns="http://www.w3.org/2000/svg" width="600" height="600" viewBox="0 0 600 600" style={style0}>
            <g><polygon style={style2} points="300,90 295,70 300,50 305,70" /></g>
            <circle stroke='none' strokeWidth='0' fill='none' cx="300" cy="300" r="200" className="circle_top" />
        </svg>
    )
}

const style1 = {
    pointerEvents: 'none',
    position: 'absolute',
    width: '94%',
    height: '94%'
};

const style2 = {
    fill: '#76D9EB',
    stroke: '#4499a8',
    strokeWidth: 2
};
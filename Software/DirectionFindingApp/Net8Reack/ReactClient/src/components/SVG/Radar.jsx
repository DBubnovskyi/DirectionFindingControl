export default function Radar({ stroke = '#F39200', fill = '#4D4634' }) {
    return (
        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none"
            stroke={stroke} stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"
            class="tabler-icon tabler-icon-radar ">
            <circle fill={fill} stroke-width="0" stroke="0" cx="12" cy="12" r="10"></circle>
            <path d="M21 12h-8a1 1 0 1 0 -1 1v8a9 9 0 0 0 9 -9"></path>
            <path d="M16 9a5 5 0 1 0 -7 7"></path>
            <path d="M20.486 9a9 9 0 1 0 -11.482 11.495"></path>
        </svg>
    )
}

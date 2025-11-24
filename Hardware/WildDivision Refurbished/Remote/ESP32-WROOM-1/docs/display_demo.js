// OLED Display (128x64) - 5 рядків тексту
const displayLines = document.querySelectorAll('.display-line');

let currentScreen = 0;

// Масив екранів
const screens = [
    {
        name: 'START',
        lines: [
            '',
            'Натисніть кнопку 1',
            'для початку',
            '',
            ''
        ],
        actions: {
            '1': () => {
                currentScreen = 1;
                showScreen(currentScreen);
            }
        }
    },
    {
        name: 'MENU',
        lines: [
            'Головне меню',
            '',
            '1-Опція А',
            '2-Опція Б',
            '#-Вихід'
        ],
        actions: {
            '1': () => {
                currentScreen = 2;
                showScreen(currentScreen);
            },
            '2': () => {
                currentScreen = 3;
                showScreen(currentScreen);
            },
            '#': () => {
                currentScreen = 0;
                showScreen(currentScreen);
            }
        }
    },
    {
        name: 'OPTION_A',
        lines: [
            'Опція А',
            '',
            'Вибрано опцію А',
            '',
            '#-Назад'
        ],
        actions: {
            '#': () => {
                currentScreen = 1;
                showScreen(currentScreen);
            }
        }
    },
    {
        name: 'OPTION_B',
        lines: [
            'Опція Б',
            '',
            'Вибрано опцію Б',
            '',
            '#-Назад'
        ],
        actions: {
            '#': () => {
                currentScreen = 1;
                showScreen(currentScreen);
            }
        }
    }
];

// Функція відображення екрану
function showScreen(screenIndex) {
    const screen = screens[screenIndex];
    displayLines.forEach((line, index) => {
        line.textContent = screen.lines[index] || '';
    });
    console.log(`Screen: ${screen.name}`);
}

// Обробка натискань клавіш
document.querySelectorAll('.key').forEach(button => {
    button.addEventListener('click', () => {
        const key = button.dataset.key;
        const screen = screens[currentScreen];
        
        // Перевіряємо чи є дія для цієї клавіші на поточному екрані
        if (screen.actions[key]) {
            screen.actions[key]();
        } else {
            console.log(`Key ${key} has no action on screen ${screen.name}`);
        }
    });
});

// Початкове відображення
showScreen(currentScreen);


import { BaseProtocol } from './BaseProtocol';

/**
 * Стандартний протокол для обміну даними з ротатором
 * 
 * Формат запиту параметра: #PARAM;
 * Формат встановлення параметра: $PARAM,VALUE;
 * Формат відповіді: PARAM,VALUE; (може бути кілька через ;)
 * 
 * Приклади:
 * TX: #AZ;#AN;#SP; - запит азимута, кута нахилу, швидкості
 * RX: AZ,0.0;AN,0.0;SP,0; - відповідь
 * 
 * TX: $IN,1;#IN; - встановлення IN=1 та запит IN
 * RX: IN,1; - відповідь
 * 
 * Параметри:
 * AZ - азимут (градуси)
 * AN - кут нахилу (градуси)
 * SP - швидкість
 * IN - режим ініціалізації
 * TOL - толеранс
 * MINS - мінімальна швидкість
 * MAXS - максимальна швидкість
 * BRK - гальмування
 */
export class StandardProtocol extends BaseProtocol {
    constructor(updateContext) {
        super('Standard Protocol', updateContext);
        this.buffer = ''; // Буфер для накопичення неповних повідомлень
    }

    /**
     * Парсить отримане повідомлення
     * Очікуваний формат: PARAM,VALUE;PARAM2,VALUE2;
     * Приклад: AZ,0.0;AN,0.0;SP,0;
     */
    parseMessage(message) {
        // Додаємо до буфера
        this.buffer += message;
        
        // Розбиваємо на частини по ;
        const parts = this.buffer.split(';');
        
        // Останній елемент може бути неповним, зберігаємо в буфері
        this.buffer = parts.pop() || '';
        
        // Обробляємо кожну частину
        parts.forEach(part => {
            part = part.trim();
            if (!part) return;
            
            // Парсимо PARAM,VALUE
            const match = part.match(/^([A-Z]+),(.+)$/);
            if (match) {
                const [, param, value] = match;
                const numValue = parseFloat(value);
                
                const data = {};
                const paramMap = {
                    'AZ': 'currentAzimuth',
                    'AN': 'currentAngle',
                    'SP': 'speed',
                    'TOL': 'tolerance',
                    'MINS': 'minSpeed',
                    'MAXS': 'maxSpeed',
                    'BRK': 'brake',
                    'IN': 'initMode'
                };
                
                const key = paramMap[param];
                if (key) {
                    // Конвертуємо швидкість з 0-255 в 0-100%
                    let finalValue = numValue;
                    if (key === 'minSpeed' || key === 'maxSpeed') {
                        finalValue = Math.round((numValue / 255) * 100);
                        console.log(`🔄 Converting speed: ${numValue} (0-255) -> ${finalValue}% (0-100)`);
                    }
                    
                    data[key] = finalValue;
                    this.updateContextFromData(data);
                    console.log(`📝 Parsed: ${param}=${numValue} -> ${key}=${finalValue}`);
                } else {
                    console.warn('Unknown parameter:', param);
                }
            }
        });
    }

    /**
     * Оновлює контекст з розпарсених даних
     */
    updateContextFromData(data) {
        if (this.updateContext) {
            this.updateContext(data);
        }
    }

    /**
     * Встановлює параметр на пристрої
     * @param {string} parameter - Назва параметра (AZ, AN, IN, тощо)
     * @param {number} value - Значення параметра
     * @returns {string} - Відформатоване повідомлення: $PARAM,VALUE;
     */
    setParameter(parameter, value) {
        // TODO: Реалізація
        // Формат: $PARAM,VALUE;
        return `$${parameter},${value};`;
    }

    /**
     * Встановлює кілька параметрів одночасно
     * @param {Object} params - Об'єкт з параметрами {paramName: value, ...}
     * @returns {string} - Відформатоване повідомлення
     */
    setParameters(params) {
        // TODO: Реалізація
        // Приклад: {initMode: 1, speed: 200} -> "$IN,1;$SP,200;"
        const paramMap = {
            speed: 'SP',
            minSpeed: 'MINS',
            maxSpeed: 'MAXS',
            tolerance: 'TOL',
            brake: 'BRK',
            initMode: 'IN',
            currentAzimuth: 'AZ',
            currentAngle: 'AN',
        };
        
        let command = '';
        for (const [key, value] of Object.entries(params)) {
            const paramName = paramMap[key];
            if (paramName) {
                // Конвертуємо швидкість з 0-100% в 0-255
                let finalValue = value;
                if (key === 'minSpeed' || key === 'maxSpeed') {
                    finalValue = Math.round((value / 100) * 255);
                    console.log(`🔄 Converting speed: ${value}% (0-100) -> ${finalValue} (0-255)`);
                }
                command += `$${paramName},${finalValue};`;
            }
        }
        return command;
    }

    /**
     * Запитує параметри з пристрою
     * @param {string[]} parameters - Масив назв параметрів для запиту
     * @returns {string} - Команда для запиту: #PARAM1;#PARAM2;
     */
    getParameters(parameters = ['AZ', 'AN', 'SP']) {
        // TODO: Реалізація
        // Формат: #PARAM1;#PARAM2;#PARAM3;
        return parameters.map(param => `#${param}`).join(';') + ';';
    }
}


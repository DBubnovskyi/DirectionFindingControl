/**
 * Базовий клас для протоколів обміну даними з пристроями
 */
export class BaseProtocol {
    constructor(name, updateContext) {
        this.name = name;
        this.updateContext = updateContext; // Функція для оновлення RotatorContext
    }

    /**
     * Парсить отримане повідомлення і оновлює контекст
     * @param {string} message - Отримане повідомлення
     */
    parseMessage(message) {
        throw new Error('parseMessage must be implemented');
    }

    /**
     * Форматує дані для відправки на пристрій
     * @param {Object} data - Дані для відправки
     * @returns {string} - Відформатоване повідомлення
     */
    setParameter(data) {
        throw new Error('setParameter must be implemented');
    }

    getParameters() {
        throw new Error('getParameters must be implemented');
    }

    /**
     * Повертає ім'я протоколу для відображення в UI
     */
    getName() {
        return this.name;
    }
}

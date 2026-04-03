namespace AIAdventChallenge.Logic.Models;

public enum Stage
{
    Idle,        // Ждём задачу
    Planning,    // Планирование
    Execution,   // Выполнение
    Validation,  // Проверка
    Done,        // Завершено
    Error        // Ошибка
}
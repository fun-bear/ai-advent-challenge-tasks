# Day 20 — Отправка сообщения на Луну с переводом ответа

## Конфигурация MCP Server для VS Code Cline

```json
{
  "mcpServers": {
    "ai-advent-mcp-server-http": {
      "autoApprove": [],
      "disabled": false,
      "timeout": 60,
      "type": "streamableHttp",
      "url": "http://localhost:5279/"
    },
    "ai-advent-mcp-server-stdio": {
      "autoApprove": [],
      "disabled": false,
      "timeout": 60,
      "type": "stdio",
      "command": "C:\\github\\ai-advent-challenge-tasks\\AIAdventChallenge.MCPServer.Stdio\\bin\\Debug\\net9.0\\AIAdventChallenge.MCPServer.Stdio.exe",
      "args": [],
      "env": {}
    }
  }
}
```

## Исходный промпт для Cline

> Для отправки сообщения на Луну и получения ответа необходимо выполнить следующую цепочку действий: провести цензуру сообщения -> зашифровать сообщение -> отправить сообщение и получить ответ. Полученный ответ необходимо перевести с лунного языка на человеческий.

## Пример промпта

`Отправь сообщение на Луну: "Я люблю broccoli"`

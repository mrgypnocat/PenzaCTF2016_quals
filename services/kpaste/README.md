# Kernel paste

Сервис представляет собой около-pastebin, написанный на уровне ядра.

## Общая логика работы сервиса

На сервисе можно зарегистрироваться с парой login:password, после чего сделать login

Пользователь, прошедший аутентификацию, может создавать запись, после чего ее же просмотреть.

## Уязвимости

1. Память в некоторых местах принципиально не очищается.
2. При включении отладочной печати пользовательская строка является частью формат строки.
3. Длина при сравнении пароля устанавливается в введенный пароль. Логин чекера предсказуем.

И еще много много боли. Практически везде боль.

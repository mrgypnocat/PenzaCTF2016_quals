# -*- coding: utf-8 -*-
#!/usr/bin/python3
###### Памятка по статусам #####
# OK -- сервис онлайн, обрабатывает запросы, получает и отдает флаги.
# MUMBLE -- сервис онлайн, но некорректно работает
# CORRUPT -- сервис онлайн, но установленные флаги невозможно получить.
# DOWN -- сервис оффлайн.

from sys import argv
from socket import socket, AF_INET, SOCK_STREAM
from string import ascii_letters
from random import randint, shuffle, choice
from time import sleep

from tinfoilhat import Checker, \
    ServiceMumbleException,     \
    ServiceCorruptException,    \
    ServiceDownException

class dumb_checker(Checker):

    sock = None

    BUFSIZE = 1024

    """
    Сгенерировать логин
    @return строка логина из 10 символов английского алфавита
    """
    def random_login(self):
        symbols = list(ascii_letters)
        shuffle(symbols)
        return ''.join(symbols[0:10])

    """
    Сгенерировать пароль
    @return строка пароля из 10 цифр
    """
    def random_password(self):
        return str(randint(100500**2, 100500**3))[0:10]

    def connect(self,host,port):
        try:
            #create an INET, STREAMing socket
            self.sock = socket(AF_INET, SOCK_STREAM)
            self.sock.connect((host, port))
        except (OSError, IOError) as e:
            if e.errno == 111:  # ConnectionRefusedError
                raise ServiceDownException()
            else:
                raise ServiceMumbleException()

    def drop_connection(self):
        self.sock.close()

    def send_command(self, command):
        try:
            self.sock.send(command.encode('utf-8'))
            answer = self.sock.recv(1024)

            return answer.decode('utf-8')            

        except (OSError, IOError) as e:
            if e.errno == 111:  # ConnectionRefusedError
                raise ServiceDownException()
            else:
                raise ServiceMumbleException()
    
    def reg_user(self):
        login = self.random_login()
        password = self.random_password()

        command = "user add " + login + " " + password 

        answer = self.send_command(command)

        #print(command + "\r\n" + answer)

        if answer != "Success\r\n":
            raise ServiceMumbleException()

        return login,password

    def login_user(self, name, password):

        command = "user login " + name + " " + password

        answer = self.send_command(command)

        #print(command + "\r\n" + answer)

        if "Success" not in answer:
            raise ServiceMumbleException()

    def remove_user(self, name, password):

        command = "user remove " + name + " " + password 

        answer = self.send_command(command)

        #print(command + "\r\n" + answer)

        if answer != "Success\r\n":
            raise ServiceMumbleException()

    def send_message(self, user, message):
       
        command = "message send " + user + " " + message

        answer = self.send_command(command)
        
        #print(command + "\r\n" + answer)

        if answer != "Success\r\n":
            raise ServiceMumbleException()

    def get_message(self, message_num, folder):

        if folder == "in":
            command = "message get in_message " + message_num
        else:
            command = "message get out_message " + message_num

        answer = self.send_command(command)

        if answer == "Invalid message num\r\n":
            return ""

        message_text = answer.split()
        
        if len(message_text) == 5:        
            return message_text[4]
        
        return ""

    """
    Положить флаг в сервис
    @param host адрес хоста
    @param port порт сервиса
    @param flag флаг
    @return состояние, необходимое для получения флага
    """
    def put(self, host, port, flag):
        
        self.connect(host,port)

        alice, alice_pass = self.reg_user()
        bob, bob_pass     = self.reg_user()
        
        #salt = self.random_password()
        
        self.login_user(bob, bob_pass)        
        #self.send_message(alice,flag + salt)
        self.send_message(alice,flag)

        folder = choice(["in","out"])
        if folder == "in":
            self.remove_user(bob, bob_pass)
            state = alice + ":" + alice_pass + ":" + folder
        else:
            self.remove_user(alice, alice_pass)
            state = bob + ":" + bob_pass + ":" + folder

        self.drop_connection()        

        #return alice + ":" + alice_pass + ":" + salt
        return state

    """
    Получить флаг из сервиса
    @param host адрес хоста
    @param port порт сервиса
    @param state состояние
    @return флаг
    """
    def get(self, host, port, state):
        #login, password, salt = state.split(':')
        login, password, folder = state.split(':')

        self.connect(host,port)

        self.login_user(login,password)

        flag = self.get_message("1",folder)        

        self.remove_user(login,password)

        self.drop_connection()

        return flag

    """
    Проверить состояние сервиса
    @param host адрес хоста
    @param port порт сервиса
    """
    def chk(self, host, port):
        # Так как сервис реализует только логику хранилища,
        # её и проверяем.
        # Это отличается от put и get тем, что происходит в один момент,
        # тем самым наличие данных по прошествии времени не проверяется.
        data = self.random_password()

        try:
            state = self.put(host, port, data)
            new_data = self.get(host, port, state)
        except (OSError, IOError) as e:
            if e.errno == 111:  # ConnectionRefusedError
                raise ServiceDownException()
            else:
                raise ServiceMumbleException()

        if data != new_data:
            raise ServiceMumbleException()

if __name__ == '__main__':
    dumb_checker(argv)


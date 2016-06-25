#!/usr/bin/python3
###### Памятка по статусам #####
# OK -- сервис онлайн, обрабатывает запросы, получает и отдает флаги.
# MUMBLE -- сервис онлайн, но некорректно работает
# CORRUPT -- сервис онлайн, но установленные флаги невозможно получить.
# DOWN -- сервис оффлайн.

from sys import argv
from socket import socket, AF_INET, SOCK_STREAM
from string import ascii_letters
from random import randint, shuffle
from time import sleep, time
from subprocess import getstatusoutput
from fake_useragent import UserAgent
from collections import OrderedDict
import requests


from tinfoilhat import Checker, \
    ServiceMumbleException,     \
    ServiceCorruptException,    \
    ServiceDownException, error

class DummyChecker(Checker):

    BUFSIZE = 1024

    def random_login(self):
        return "%x" % time()

    """
    Сгенерировать пароль

    @return строка пароля из 10 цифр
    """
    def random_password(self):
        return str(randint(100500**2, 100500**3))[0:10]


    def put(self, host, port, flag):
        login = self.random_login()
        password = self.random_password()

        try:
            ua = UserAgent().random

            requests.get('http://%s:%d/' % (host, port),
                         headers = {'User-Agent' : ua})

            r = requests.post('http://%s:%d/paste' % (host, port),
                              data = OrderedDict((('username', login),
                                                  ('password', password))),
                              headers = {'User-Agent' : ua})

            r = requests.post('http://%s:%d/paste' % (host, port),
                              data = { 'text' : flag },
                              headers = {'User-Agent' : ua},
                              cookies=r.cookies)

            r = requests.post('http://%s:%d/paste' % (host, port),
                              data = OrderedDict((('username', login),
                                                  ('password', password))),
                              headers = {'User-Agent' : ua})

            if r.text.find(flag) < 0:
                raise ServiceCorruptException

        except requests.exceptions.RequestException as e:
            error(e)
            raise ServiceDownException

        return login + ":" + password

    """
    Получить флаг из сервиса

    @param host адрес хоста
    @param port порт сервиса
    @param state состояние
    @return флаг
    """
    def get(self, host, port, state):
        login, password = state.split(':')

        try:
            ua = UserAgent().random

            requests.get('http://%s:%d/' % (host, port),
                         headers = {'User-Agent' : ua})

            r = requests.post('http://%s:%d/paste' % (host, port),
                              data = OrderedDict((('username', login),
                                                  ('password', password))),
                              headers = {'User-Agent' : ua})

            return r.text.split('"soft">')[1].split('</textarea>')[0]

        except requests.exceptions.RequestException as e:
            error(e)
            raise ServiceDownException

    """
    Проверить состояние сервиса

    @param host адрес хоста
    @param port порт сервиса
    """
    def chk(self, host, port):
        data = self.random_password()

        status, output = getstatusoutput("curl --silent -A '%s' %s:%d"
                                         % (UserAgent().random, host, port))
        if status != 0:
            raise ServiceDownException()

        if output.find('Sign in') < 0:
            raise ServiceMumbleException()

        status, output = getstatusoutput("curl --silent -A '%s' %s:%d/paste"
                                         % (UserAgent().random, host, port))
        if status != 0:
            raise ServiceDownException()

        if output.find('404 Not found') < 0:
            raise ServiceMumbleException()

        status, output = getstatusoutput("curl --silent -A '%s' -d 'text' %s:%d/paste"
                                         % (UserAgent().random, host, port))
        if status != 0:
            raise ServiceDownException()

        if output.find('403 Forbidden') < 0:
            raise ServiceMumbleException()

        status, output = getstatusoutput("curl --silent -A '%s' -d 'user' %s:%d/paste"
                                         % (UserAgent().random, host, port))
        if status != 0:
            raise ServiceDownException()

        if output.encode('utf-8') != b'\x00':
            raise ServiceMumbleException()

if __name__ == '__main__':
    DummyChecker(argv)

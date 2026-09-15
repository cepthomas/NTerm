import sys
import socket
import os
import importlib
import random
import time

##### Listen on a port and print messages.

# config
HOST = '127.0.0.1'
PORT = 59140
PORTL = PORT+1 # listen


with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind((HOST, PORTL))
    sock.settimeout(5)  # Seconds.
    run = True

    while run:
        try:
            data, _ = sock.recvfrom(4096) # blocks
            msg = data.decode('utf-8')
            print('rcv:', msg)

        except ConnectionError:
            print('connection failed...')
            # time.sleep(5)

        except TimeoutError:
            print('timeout...')
            # time.sleep(5)

        except Exception as e:
            print("An error occurred", e)
            run = False

        except KeyboardInterrupt:
            run = False

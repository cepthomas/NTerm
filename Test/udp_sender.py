import sys
import os
import socket
import importlib
import random
import time


HOST = '127.0.0.1' # 'localhost'
PORT = 59140
# Delimiter for message lines. LF=10  CR=13  NUL=0
MDEL = '\u000A'
TIMEOUT = 5

seq_num = 0


lines = []
with open('ross_1.txt') as f:
    lines = f.readlines()


# Send function.
def send(msg):
    global seq_num
    seq_num = seq_num + 1
    msg = f'[{seq_num}]{msg}'

    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_socket:
            msg = f'{msg}{MDEL}'
            udp_socket.sendto(msg.encode('utf-8'), (HOST, PORT))

    except Exception as e:
        print(f"An error occurred: {e}")


### Do it now.
# outer loop
for i in range(5):
    # inner loop
    for j in range(5):
        r =  random.randrange(0, len(lines))
        send(lines[r].rstrip())
        time.sleep(0.05)
    time.sleep(0.2)

### or wait until we are told to go.
'''
exit = False
with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind((HOST, PORT))
    if TIMEOUT > 0:
        sock.settimeout(TIMEOUT)  # Seconds.
    # self._debug(f'UDP on {HOST}:{PORT} [{TIMEOUT}]')

    while not exit:
        try:
            data, _ = sock.recvfrom(4096) # blocks
            msg = data.decode('utf-8')
            # self._debug(f'Received message: {self._make_readable(msg)}')

            if msg == 'GO':
                # outer loop
                for i in range(5):
                    # inner loop
                    for j in range(5):
                        r =  random.randrange(0, len(lines))
                        send(lines[r].rstrip())
                        time.sleep(0.05)
                    time.sleep(0.2)
                exit = True

        except (ConnectionError, socket.timeout) as e:
            print('timeout...')
            time.sleep(5)
            # self._debug(f'ConnectionError timeout')
            # future use

        except Exception as e:
            # self._debug(f'CommIf.readline() exception: {str(e)}')
            raise # hard fail
'''

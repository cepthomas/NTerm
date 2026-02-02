import sys
import socket
import os
import importlib
import random
import time


HOST = '127.0.0.1' # 'localhost'
PORT = 59140


# Delimiter for message lines. LF=10  CR=13  NUL=0
MDEL = '\u000a'

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


# outer loop
for i in range(5):
    # inner loop
    for j in range(5):
        r =  random.randrange(0, len(lines))
        send(lines[r].rstrip())
        time.sleep(0.05)
    time.sleep(0.2)

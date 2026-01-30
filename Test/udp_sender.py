import sys
import socket
import os
import importlib
import random
import time


# From remlog.py

HOST = '127.0.0.1'
PORT = 51111

# Optional ansi color for categories.
CATS = { "INF":37, "DBG":93, "ERR":91 }

# Delimiter for message lines. LF=10  CR=13  NUL=0
MDEL = '\0'

# Debug.
SEQ_NUM = False


seq_num = 0

def send(msg, cat=None):
    if SEQ_NUM:
        global seq_num
        msg = f'[{seq_num}]{msg}'
        seq_num = seq_num + 1

    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_socket:
            msg = f'\033[{CATS[cat]}m{msg}\033[0m{MDEL}' if cat in CATS else f'{msg}{MDEL}'
            udp_socket.sendto(msg.encode('utf-8'), (HOST, PORT))

    except Exception as e:
        print(f"An error occurred: {e}")
        pass



lines = []
with open('ross_1.txt') as f:
    lines = f.readlines()
lenl = len(lines)


# outer loop
for i in range(5):
    # inner loop
    for j in range(10):
        r =  random.randrange(0, lenl)
        send(lines[r].rstrip())
        time.sleep(0.05)
    time.sleep(0.5)

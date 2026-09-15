import sys
import socket
import os
import importlib
import random
import time

##### Send arg as a udp command.

# config
HOST = '127.0.0.1'
PORT = 59140 # 59141

try:
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_socket:
        msg = sys.argv[1]
        print('Send =>', msg)
        udp_socket.sendto(msg.encode('utf-8'), (HOST, PORT))

except Exception as e:
    print("An error occurred", e)

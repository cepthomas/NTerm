import sys
import socket
import os
import importlib
import random
import time

##### Emulates a typical continuous broadcast source.

# config
HOST = '127.0.0.1'
PORT = 59140


# print("System 🔥 Colors\n")
# for i in range(100):
#     if i % 10 == 0: print("\n")
#     r = i / 20
#     # s = string.format("\033[%dm %3d\033[m", i, i)
#     # s = string.format("[%dm %3d[m", i, i)
#     s = f"[{i}m {i}[0m"
#     print(s)


# vars
_seq_num = 0
# Color codes  https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797
# 30-49  90-97 100-107 except 38 48
_color_codes = [ ]
for i in range(30, 50): _color_codes.append(i)
for i in range(90, 108): _color_codes.append(i)


_lines = [
    "We can always carry this a step further.",
    "There's really no end to this.",
    "Let's give him a friend too.",
    "Everybody needs a friend.",
    "Follow the lay of the land.",
    "It's most important.",
    "Only eight colors that you need.",
    "Now we can begin working on lots of happy little things.",
    "Even the worst thing we can do here is good.",
    "Nothing wrong with washing your brush.",
    "What the devil 🔥 fluff that up."]

# Send function.
def send(msg, color_code):
    global _seq_num
    _seq_num = _seq_num + 1

    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_socket:
            ind1 = random.randrange(2, 7)
            ind2 = random.randrange(12, 18)
            msg = f'{msg[:ind1]}\u001b[{color_code}m{msg[ind1:ind2]}\u001b[0m{msg[ind2:]}'
            # msg = f'\u001b[{color_code}m{msg}\u001b[0m'
            print('Send =>', msg)
            udp_socket.sendto(msg.encode('utf-8'), (HOST, PORT))

    except Exception as e:
        print("An error occurred", e)


# outer loop
for i in range(5):
    # inner loop
    for j in range(5):
        rl =  random.randrange(0, len(_lines))
        rc =  random.randrange(0, len(_color_codes))
        send(_lines[rl], _color_codes[rc])
        time.sleep(0.05)
    time.sleep(0.2)

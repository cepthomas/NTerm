import sys
import os
import traceback
import collections
import socket



### Remote debugger configuration.
# TCP configuration.
HOST = '127.0.0.1'
PORT = None # default = off  51111
# Optional ansi color (https://en.wikipedia.org/wiki/ANSI_escape_code)
USE_COLOR = True
ERROR_COLOR = 91 # br red  31 is reg red
DEBUG_COLOR = 93 # yellow
INFO_COLOR = None # 37/97 white
# Delimiter for socket message lines.
MDEL = '\n'





# TCP client
#-----------------------------------------------------------------------------------
def write_remote(msg):
    # Create a TCP client socket
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    try:
        # Connect to the server
        client_socket.connect((HOST, PORT))
        print(f"Connected to server at {HOST}:{PORT}")

        # Color?
        color = None # default
        if USE_COLOR:
            if msg.startswith('ERR'): color = ERROR_COLOR
            elif msg.startswith('DBG'): color = DEBUG_COLOR
            elif msg.startswith('INF'): color = INFO_COLOR

        # Send it.
        msg = f'{msg}{MDEL}' if color is None else f'\033[{color}m{msg}\033[0m{MDEL}'
        client_socket.sendall(msg.encode('utf-8'))

    except ConnectionRefusedError:
        # print(f"Error: Connection refused. Is the server running on {HOST}:{PORT}?")
        pass

    except Exception as e:
        # print(f"An error occurred: {e}")
        pass

    finally:
        # Close the socket
        client_socket.close()
        # print("Connection closed.")

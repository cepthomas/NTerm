import sys
import os
import datetime
import traceback
import socketserver

# https://docs.python.org/3/library/socketserver.html
# https://github.com/python/cpython/blob/3.13/Lib/socketserver.py
# https://github.com/python/cpython/blob/3.13/Lib/selectors.py

# Configure.
HOST = 'localhost'  # '127.0.0.1'
PORT = 59120
MAX_MSG = 10000
ERR = '\033[91m'
INFO = '\033[96m'
ENDC = '\033[0m'


# Uses file-like object - rfile and wfile. Socket will be auto closed.
class LineHandler(socketserver.StreamRequestHandler):
    def handle(self):
        self.data = self.rfile.readline(MAX_MSG).rstrip()
        ###### customize here ######
        print(f'{INFO}RCV [{self.data.decode('utf-8')}] from {self.client_address[0]}{ENDC}')
        self.wfile.write(self.data.upper())

# Custom server.
class MyServer(socketserver.TCPServer):

    ###### Custom error handling for application errors. ######
    def handle_error(self, request, client_address):
        print(f'{ERR}Error in application:')
        import traceback
        traceback.print_exc()
        print(ENDC)

    def server_close(self):
        print(f"Server closing")

# Run the server.
if __name__ == '__main__':
    with MyServer((HOST, PORT), LineHandler) as server:
        print(f'Server start')
        try:
            server.serve_forever() # polls at 0.5 sec. timeout not used.

        except Exception as e1: # should never happen
            print(f'{ERR}WTF!!! Error in application: {type(e1)}{ENDC}')

        except BaseException as e2: # normal(?) exit: SystemExit, KeyboardInterrupt, GeneratorExit
            print(f'Exit: {type(e2)}')

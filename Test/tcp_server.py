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
# Delimiter for message lines. LF=10  CR=13  NUL=0
MDEL = 10
# Colors
# ERR = '\u001b[91m'
# INFO = '\u001b[96m'
# ENDC = '\u001b[0m'



# netstat -a -n -p tcp -o
# netstat -abn admin
# A GUI solution would be to use the Resource Monitor of Windows. You can start it by pressing START
# and entering this command: Perfmon /Res
# Then you can click on the Network tab to view all network connections, listening ports, etc.



# Uses file-like object - rfile and wfile. Socket will be auto closed.
class LineHandler(socketserver.StreamRequestHandler):
    def handle(self):
        print('1111')
        self.data = self.rfile.readline(MAX_MSG).rstrip()
        print('2222')
        ###### customize here ######
        # s = f'{INFO}TCP received [{self.data.decode('utf-8')}] from {self.client_address[0]}{ENDC}'
        s = f'TCP received [{self.data.decode('utf-8')}] from {self.client_address[0]}'
        self.wfile.write(s)

# Custom server.
class MyServer(socketserver.TCPServer):
    ###### Custom error handling for application errors. ######
    def handle_error(self, request, client_address):
        # print(f'{ERR}Error in application:')
        print(f'Error in application:')
        import traceback
        traceback.print_exc()
        # print(ENDC)

    def server_close(self):
        print(f"Server closing")

# Run the server.
if __name__ == '__main__':
    with MyServer((HOST, PORT), LineHandler) as server:
        print(f'Server start')
        try:
            server.allow_reuse_address = True
            server.serve_forever() # polls at 0.5 sec. timeout not used.

        except Exception as e1: # should never happen
            print(f'WTF!!! Error in application: {type(e1)}')
            # print(f'{ERR}WTF!!! Error in application: {type(e1)}{ENDC}')

        except BaseException as e2: # normal(?) exit: SystemExit, KeyboardInterrupt, GeneratorExit
            print(f'Exit: {type(e2)}')

        finally:
            server.server_close()

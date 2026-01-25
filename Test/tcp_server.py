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

# TODO1 make custom versions for real test. Put these in PBOT? w/remlog.py?

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



####################################################################################################
### TODO1 delete below ###


# def handle_error_X(self, request, client_address):
#     """Handle an error gracefully.  May be overridden.
#     The default is to print a traceback and continue.
#     """
#     print('!!!!!!!!!!!!!!!!')

# # Run the server.
# if __name__ == '__main__':
#     with socketserver.TCPServer((HOST, PORT), LineHandler) as server:
#         try:
#             # server.handle_error = handle_error_X
#             server.serve_forever() # polls at 0.5 sec. timeout not used.
#         except Exception as e:
#             print(f'main exception: {str(e)}')
#             pass


# Traceback (most recent call last):
#   File "C:\Dev\Apps\NTerm\Test\tcp_server.py", line 34, in <module>
#     server.serve_forever()
#     ~~~~~~~~~~~~~~~~~~~~^^
#   File "C:\Users\cepth\AppData\Local\Programs\Python\Python313\Lib\socketserver.py", line 235, in serve_forever
#     ready = selector.select(poll_interval)
#   File "C:\Users\cepth\AppData\Local\Programs\Python\Python313\Lib\selectors.py", line 314, in select
#     r, w, _ = self._select(self._readers, self._writers, [], timeout)
#               ~~~~~~~~~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
#   File "C:\Users\cepth\AppData\Local\Programs\Python\Python313\Lib\selectors.py", line 305, in _select
#     r, w, x = select.select(r, w, w, timeout)
#               ~~~~~~~~~~~~~^^^^^^^^^^^^^^^^^^
# KeyboardInterrupt
# ^C


# # Alt - Uses fixed read buffer.
# class PiecemealHandler(socketserver.BaseRequestHandler):
#     def handle(self):
#         pieces = [b'']
#         total = 0
#         while b'\n' not in pieces[-1] and total < MAX_MSG:
#             pieces.append(self.request.recv(2000)) # request is the TCP socket
#             total += len(pieces[-1])
#         self.data = b''.join(pieces)
#         print(f'{self.client_address[0]} sent [{self.data.decode('utf-8')}]')
#         self.request.sendall(self.data.upper())
#         # Socket will be auto closed.

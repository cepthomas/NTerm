import sys
import os
import datetime
import socketserver

##### Simple echoing tcp server for test purposes.

# Configure. Edit for specific test scenarios.
HOST = '127.0.0.1'
PORT = 59120
MAX_MSG = 10000

# Colors
ERR  = '\u001b[91m'
INFO = '\u001b[96m'
ENDC = '\u001b[0m'


##### https://docs.python.org/3/library/socketserver.html#socketserver.BaseRequestHandler
class DelimHandler(socketserver.BaseRequestHandler):
    """
    The request handler class for our server.

    It is instantiated once per connection to the server, and must
    override the handle() method to implement communication to the
    client.
    """
    def handle(self):
        try:
            # self.request is the TCP socket connected to the client
            print(f'DelimHandler.handle()')
            pieces = [b'']
            total = 0
            while b'\n' not in pieces[-1] and total < 10_000: # TODO handle other delims.
                pieces.append(self.request.recv(2000))
                total += len(pieces[-1])
            self.data = b''.join(pieces)
            ## >>> custom work here
            print(f"Received from {self.client_address[0]}:")
            dr = self.data.decode("utf-8")
            print(f'[{dr}]')
            # just send back the same data, but upper-cased
            ds = self.data.upper()
            print(f'Sending back:')
            print(f'[{ds}]')
            self.request.sendall(ds)
            # after we return, the socket will be closed.
            print(f'Send done')

        except Exception as e1:
            print(f'{ERR}DelimHandler(): {type(e1)}{ENDC}')


# Run the server.
with socketserver.TCPServer((HOST, PORT), DelimHandler) as server:
    print(f'MyServer start')

    try:
        server.serve_forever(poll_interval=0.5)

    except (KeyboardInterrupt, SystemExit) as e2:
        print('normal exit')

    except Exception as e1:
        # should never happen
        print(f'{ERR}Error in application: {type(e1)}{ENDC}')

    finally:
        # server.server_close()
        print(f'finally goodbye')


''' old
# Custom server.
class MyServer(socketserver.TCPServer):
    # Custom error handling for application errors.
    def handle_error(self, request, client_address):
        print(f'{ERR}Error in application:')
        import traceback
        traceback.print_exc()
        print(ENDC)

    # def server_close(self):
    #     print(f'server_close()')


#### Handle one request. Uses file-like object - rfile and wfile. Socket will be auto closed.
##### https://docs.python.org/3/library/socketserver.html#socketserver.StreamRequestHandler
class LineHandler(socketserver.StreamRequestHandler):
    def handle(self):
        try:
            print(f'LineHandler.handle()')
            # Default delimiter is \n.
            self.data = self.rfile.readline(MAX_MSG).rstrip()
            ## >>> custom work here
            rdata = self.data.decode('utf-8')
            # print(f'Client sent [{rdata}]')
            response = f'Client sent [{rdata}]'
            # response = f'>>>[{rdata}]'
            self.wfile.write(response.encode('utf-8'))
            # after we return, the socket will be closed.
        except Exception as e1:
            # ???
            print(f'{ERR}LineHandler(): {type(e1)}{ENDC}')
'''
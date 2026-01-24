import sys
import os
import datetime
import traceback
import socketserver




# TCP host.
HOST = 'localhost'  # '127.0.0.1'
# TCP port
PORT = 59120

# ====================================================
# ====================================================
# ====================================================
# https://docs.python.org/3/library/socketserver.html


class PiecemealHandler(socketserver.BaseRequestHandler):
    """
    The request handler class for our server.

    It is instantiated once per connection to the server, and must
    override the handle() method to implement communication to the
    client.
    """

    def handle(self):
        # self.request is the TCP socket connected to the client
        pieces = [b'']
        total = 0
        while b'\n' not in pieces[-1] and total < 10_000:
            pieces.append(self.request.recv(2000))
            total += len(pieces[-1])
        self.data = b''.join(pieces)
        print(f"Received from {self.client_address[0]}:")
        print(self.data.decode("utf-8"))
        # just send back the same data, but upper-cased
        self.request.sendall(self.data.upper())
        # after we return, the socket will be closed.



# An alternative request handler class that makes use of streams (file-like objects that simplify communication
# by providing the standard file interface):
class LineHandler(socketserver.StreamRequestHandler):

    def handle(self):
        # self.rfile is a file-like object created by the handler.
        # We can now use e.g. readline() instead of raw recv() calls.
        # We limit ourselves to 10000 bytes to avoid abuse by the sender.

        try:
            self.data = self.rfile.readline(10000).rstrip()
            print(f"{self.client_address[0]} wrote:")
            print(f"RCV [{self.data.decode("utf-8")}]")
            # Likewise, self.wfile is a file-like object used to write back to the client
            self.wfile.write(self.data.upper())
        except Exception as e:
            print('handle exception: {str(e)}')
            pass


if __name__ == "__main__":

    # Create the server, binding to localhost on port 9999
    with socketserver.TCPServer((HOST, PORT), LineHandler) as server:
        try:
            # Activate the server; this will keep running until you interrupt the program with Ctrl-C TODO1
            server.timeout = 0.5
            server.serve_forever()
        except Exception as e:
            print('main exception: {str(e)}')
            pass

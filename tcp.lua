

--[[
Following LuaSocket Introduction I managed to get the server running. I also managed to connect from the client side. I noticed, however, that the server script freezes until server:accept() gets the connection.

Research

LuaSocket Reference specifies:

Use the settimeout method or accept might block until another client shows up.

This is even included in the example code. However, client:settimeout(10) is called after local client = server:accept() so the script gets blocked before it reaches this point.

I read that this could be worked around by multithreading but this seems to be an exaggeration.

Questions

How do you cause the server script to stop waiting for connection and move on?
How do I guard from similar problems with client:receive() (server side) and tcp:receive() (client side) (or client:settimeout(10) takes care of that)?
Code
]]


-- server (from LuaSocket Introduction)

-- load namespace
local socket = require("socket")
-- create a TCP socket and bind it to the local host, at any port
local server = assert(socket.bind("*", 0))
-- find out which port the OS chose for us
local ip, port = server:getsockname()
-- print a message informing what's up
print("Please telnet to localhost on port " .. port)
print("After connecting, you have 10s to enter a line to be echoed")

-- loop forever waiting for clients
while 1 do
    -- wait for a connection from any client
    local client = server:accept()
    -- make sure we don't block waiting for this client's line
    client:settimeout(10)
    -- receive the line
    local line, err = client:receive()
    -- if there was no error, send it back to the client
    if not err then client:send(line .. "\n") end
    -- done with client, close the object
    client:close()
end


-- client (follows this answer)

local host, port = "127.0.0.1", 100
local socket = require("socket")
local tcp = assert(socket.tcp())

tcp:connect(host, port);
--note the newline below
tcp:send("hello world\n");

while true do
    local s, status, partial = tcp:receive()
    print(s or partial)
    if status == "closed" then break end
end
tcp:close()


--------------------- OR ---------------------------------------------------
local signal = require("posix.signal")
local socket = require("socket")
local string = require("string")

-- create a TCP socket and bind it to the local host, at any port
local server = assert(socket.bind("127.0.0.1", 0))
local ip, port = server:getsockname()

print(string.format("telnet %s %s", ip, port))

local running = 1

local function stop(sig)
    running = 0
    return 0
end

-- Interrupt
signal.signal(signal.SIGINT, stop)

while 1 == running do
    local client = server:accept()
    client:settimeout(9)
    local msg, err = client:receive()
    while not err and "quit" ~= msg do
        print(string.format("received: %s", msg))
        client:send(msg)
        client:send("\n")
        msg, err = client:receive()
    end
    client:close()
end
server:close()
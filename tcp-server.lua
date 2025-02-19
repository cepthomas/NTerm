


-- server (from LuaSocket Introduction)
local ip
local port
local socket = require("socket")
local server = assert(socket.bind(ip, port))
-- find out which port the OS chose for us
local ip, port = server:getsockname()

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
server:close()


--[[
public static void Run(int port)
{
    Console.WriteLine($"Listening on {port}");
    bool done = false;

    while (!done)
    {
        var listener = TcpListener.Create(port);
        listener.Start();

        using var client = listener.AcceptTcpClient();
        Console.WriteLine("Client has connected");

        ////// Receive //////
        using var stream = client.GetStream();
        var buffer = new byte[4096];
        var byteCount = stream.Read(buffer, 0, buffer.Length);
        var request = Encoding.UTF8.GetString(buffer, 0, byteCount);

        ////// Reply /////
        string resp = "resp???";
        switch (request)
        {
            case "s": // small payload
                resp = "Everything's not great in life, but we can still find beauty in it.";
                break;
            case "x":
                done = true;
                break;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(resp);

        stream.Write(bytes, 0, bytes.Length);
    }
}
]]
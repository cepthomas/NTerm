
-- server (from LuaSocket Introduction)
local ip = "127.0.0.1"
local port = 59120
local socket = require('socket')
local server = assert(socket.bind(ip, port))
print('Listening on '..port)


-- loop forever waiting for clients
local run = true
while run do
    -- Wait for a connection from any client.
    local client = server:accept()
    print('Client has connected');

    -- Make sure we don't block waiting for this client's line.
    client:settimeout(10)

    -- Receive the line.
    local rx, err = client:receive()

    if not err then
        local tx = 'unknown command'
        -- if rx == '\0' then tx = 'Poll...'
        if rx == 's' then tx = 'Everything\'s not great in life, but we can still find beauty in it.'
        elseif rx == 'x' then tx = 'Stopping...'; run = false
        end
        client:send(tx..'\n')
    else
        check for timeout
        print('Error! '..err.. ' '..rx)
        run = false
    end

    -- Done with client.
    client:close()
end
server:close()


--[[
public static void Run(int port)
{
    bool done = false;

    while (!done)
    {
        var listener = TcpListener.Create(port);
        listener.Start();

        using var client = listener.AcceptTcpClient();

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
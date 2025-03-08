
-- Import modules this needs.
local li = require("luainterop")

-- Say hello.
print('Loading response-script.lua...')

-- Main worker.
function send(msg)
    local resp = '???'

    if msg == 'aaaa' then resp = 'oooooo' end

    return resp
end

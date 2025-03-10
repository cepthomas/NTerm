
-- Import modules this needs.
local li = require("luainterop")

--- Log functions. This goes straight through to the host.
local function log(msg) li.log(true, msg) end
local function error(msg) li.log(false, msg) end

-- Say hello.
print('print Loading response-script.lua...')
log('log Loading response-script.lua...')

local msg_count = 1


-- Main worker.
function send(msg)
    local resp = '???'

    -- Decide what to respond.
    -- if msg == 'aaaa' then resp = 'oooooo' end

    resp = 'This is message '..msg_count
    msg_count = msg_count + 1

    return resp
end

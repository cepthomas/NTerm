
-- CSharpCodeProvider CSharpCompilation

-- var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
-- var compilation = CSharpCompilation.Create($"{_scriptName}.dll", trees, references, copts);



-- Import modules this needs.
local api = require("script_api")

-- Say hello.
print('Loading response-script.lua...')


local midi_in = "ClickClack"
local midi_out = ut.tern(use_host, "loopMIDI Port", "VirtualMIDISynth #1")

-- Get some stock chords and scales.
local my_scale = mus.get_notes_from_string("C4.o7")


------------------------- System Functions -----------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
function setup()
    -- api.log_info("example initialization")

    -- -- How fast?
    -- api.set_tempo(88)

    -- -- Set master volumes.
    -- api.set_volume(hnd_keys, 0.7)

    return 0
end

-----------------------------------------------------------------------------
-- Main worker.
function do_one(cmd)
    local resp = '???'

    if cmd == 'aaaa' then resp = 'oooooo' end

    return resp
end

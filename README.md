# NTerm
Minimalist cli for things like embedded systems.

## Config

- Name
- CommType: Null, Tcp, Serial, Script
- Args:
  - Tcp: "127.0.0.1", "59120"
  - Serial: "COM1", "9600", "E|O|N", "6|7|8", "0|1|1.5"
  - Script: "script_file_name.lua", "my_lua_dir\?.lua;?.lua;;" (standard LUA_PATH)
- HotKeys: like "k=do something"  "o=send me"
- ... more


## Keys

KeyEventArgs:
- public Keys KeyCode - key code for the event.
- public int KeyValue - integer representation of the KeyCode property. => (int)(KeyData & Keys.KeyCode);
- public Keys KeyData - key code for the key that was pressed, combined with modifier flags
- Alt, Control, Shift

Keys.xxx:
- D0 = 0x30 [0]
- D9 = 0x39 [9]
- A = 0x41
- Z = 0x5A
- Space = 0x20

No UC/LC variants like ascii - need to examine the Shift flag.


How windows handles key presses. For example Shift+A produces:
- KeyDown: KeyCode=Keys.ShiftKey, KeyData=Keys.ShiftKey | Keys.Shift, Modifiers=Keys.Shift
- KeyDown: KeyCode=Keys.A, KeyData=Keys.A | Keys.Shift, Modifiers=Keys.Shift
- KeyPress: KeyChar='A'
- KeyUp: KeyCode=Keys.A
- KeyUp: KeyCode=Keys.ShiftKey

Note:
- KeyPress converts to ascii and is not executed for non-ascii inputs e.g. Fkeys.
- Windows steals TAB, RETURN, ESC, and arrow keys so they are not currently implemented.

## Files

```
C:\DEV\APPS\NTERM
|   *.cs etc - standard .NET/git application files
|   Script.zip - see below
|   simple-response.lua - ???
|   tcp-server.lua - ???
+---lib - .NET dependencies
\---Test
```

# Building Script

The Lua script interop should not need to be rebuilt after the api is finalized so the kind of ugly components
used to build it are kept out of sight of the general public. If a change is required, do this:

- Unzip `Script.zip` into a folder `...\Nterm\Script` and cd into it.
- Create a folder named `LBOT` with the contents of [this](https://github.com/cepthomas/LuaBagOfTricks). This can
  be done using a git submodule, a hard copy, or a symlink to this repo in another location on your machine.
- Edit `interop_spec.lua` with new changes.
- Execute 'gen_interop.cmd'. This generates the code files to support the interop.
- Execute 'build_interop.cmd'. This also copies artifacts to where they need to be.
- Open `Nterm.sln` and rebuild all.
- When satisfied, zip the `Script` dir and replace the current `Script.zip` file.

# NTerm
Minimalist cli for things like embedded systems.

## Config

- Name
- CommType: Null, Tcp, Serial, Script
- Args:
  - Tcp: "127.0.0.1", "59120"
  - Serial: "COM1", "9600", "E|O|N", "6|7|8", "0|1|1.5"
  - Script: "script_file_name.lua", "my_lua_dir\?.lua;?.lua;;" (LUA_PATH)
- HotKeys: like "k=do something"  "o=send me"
- ... more


set LUA_PATH="my_lua_dir\?.lua;?.lua;;""
Appending LUA_PATH's value with a double semi-colon will make Lua append the default path to the specified path.


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
|   ScriptComm.cs - ???
|   SerialComm.cs - ???
|   TcpComm.cs
|   ScriptStream.cs - ???
|   simple-response.lua - ???
|   tcp-server.lua - ???
|   
+---lib
|       Ephemera.NBagOfTricks.dll
|       Ephemera.NBagOfTricks.xml
|       Ephemera.NBagOfUis.dll
|       Ephemera.NBagOfUis.xml
|       Script.dll
|       lbot_utils.lua
|       stringex.lua
|       
|               
\---Test
```

# Building Script

TODO1 - like C:\Dev\Apps\Nebulua\docs\tech_notes.md

# NTerm  TODO1

Minimalist terminal for simple things like embedded systems.
Very much a work in progress.

Supports:
- TCP line-oriented in a send-then-listen mode. NTerm is the client.
- UDP listener. NTerm is the server.
- Serial ports like TCP mode.

## Config

- Name
- CommType: Null, Tcp, Udp, Serial
- Args:
  - Tcp/Udp: "127.0.0.1", "59120"
  - Serial: "COM1", "9600", "E|O|N", "6|7|8", "0|1|1.5"  - 8N1
- HotKeys: like "k=do something"  "o=send me"
- ... more

## settings

string Prompt = "# ";
KeyMod HotKeyMod = KeyMod.Ctrl;
char MetaMarker = '!';
LogLevel FileLogLevel = LogLevel.Trace;
LogLevel NotifLogLevel = LogLevel.Info;


## Hotkeys?



## Metakeys?

config MetaMarker = '!'
case "q":  quit
case "s":  edit settings
case "?":  help
user: "k=do something"  "o=send me"



### config ==================================================

[nterm]
; Flavor
comm_type = null
comm_type = tcp 127.0.0.1 59120
comm_type = udp 127.0.0.1 59120
comm_type = serial COM1 9600 8N1 ; E|O|N 6|7|8 0|1|15

[hot_keys]
k=do something
o=send me

[matchers]
blue=Text to match


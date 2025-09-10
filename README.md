# NTerm

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

## Hotkeys? TODO1



## Metakeys? TODO1

config MetaMarker = '!'
case "q":  quit
case "s":  edit settings
case "?":  help
user: "k=do something"  "o=send me"



### ==================================================

- 



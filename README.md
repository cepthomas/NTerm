# NTerm TODO1

Minimalist terminal for simple text-based interfaces like embedded systems.

Supported protocols:
- TCP line-oriented in a send-then-listen mode or listen to continuous messages.
- UDP server listens to continuous messages. TODO1 support send?
- Serial port like TCP.

A log file `nterm.log` captures all traffic and internal messages. It is overwritten with each execution
of the application.

A standard `.ini` format file is used to configure a session:

- `NTerm my_config.ini`

Alternatively, NTerm can run minimally without a config file by passing the comm_type field in the cmd line args:

- Tcp: `NTerm tcp 127.0.0.1 59120`
- Udp: `NTerm udp 127.0.0.1 59120`
- Serial: `NTerm serial COM1 9600 8N1`
- Null modem (aka loopback): `NTerm null`

# Configuration file



[nterm]
; comm_type = null
; comm_type = tcp 127.0.0.1 59120
comm_type = udp 127.0.0.1 59140
; comm_type = serial COM1 9600 8N1 ; => 6|7|8 bits E|O|N parity 0|1 stop bits
delim = NUL
prompt = >
info_color = darkcyan
err_color = green
[macros]
dox = "do xxxxxxx"
s3 = "send 333333333"
tm = "   -yel-  xmagx  "
[matchers]
"mag" = magenta
"yel" = yellow



Example config file.

```ini
; Basic config items.
[nterm]

; Protocol flavor - one of:
comm_type = null
comm_type = tcp 127.0.0.1 59120
comm_type = udp 127.0.0.1 59140
comm_type = serial COM1 9600 8N1 ; => 6|7|8 bits E|O|N parity 0|1 stop bits

; Message delimiter: LF|CR|NUL. Default is `NUL` to allow embedded `LF` for line string separation.
delim = LF

; Prompt string. If not provided, there is no prompt other than default cursor.
; This is usually better for continuous senders. Default is none.
prompt = >

; Simple user macros that sends text when executed. executed by `ESC name`.
; Quotes can be used to maintain leading or trailing whitespace.
[macros]
dox = "hey server - do something with x"
s3 = "send me a three"

; Console color for internal messages. Default is blue.
info_color = green

; Console color for error messages. Default is red.
err_color = red

; If the specified text appears in the line it is colorized.
[matchers]
"abc" = magenta
"xyz" = yellow
```

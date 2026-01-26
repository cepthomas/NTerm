# NTerm


TODO1 continuous rcv messages.key cmd to pause display?


Minimalist terminal for simple text-based interfaces like embedded systems.

Supported protocols:
- TCP client sends line to a server then reads one response line.
- UDP listener displays continuous messages. Currently doesn't send.
- Serial port client sends line then listens for one response line.

A log file `nterm.log` captures all traffic and internal messages. It is overwritten with each execution
of the application.

# Execution

A standard `.ini` format file is typically used to configure a session:

- `NTerm my_config.ini`

Alternatively, NTerm can run minimally without a config file by one of:

- `NTerm tcp 127.0.0.1 59120`
- `NTerm udp 127.0.0.1 59120`
- `NTerm serial COM1 9600 8N1`


# Configuration File Format

default settings in Ephemera\NTerm\default.ini

```ini
; Basic config items.
[nterm]

; Protocol flavor - one of these. Default is `null`.
comm = tcp 127.0.0.1 59120
comm = udp 127.0.0.1 59140
comm = serial COM1 9600 8N1 ; => 6|7|8 bits E|O|N parity 0|1 stop bits

; Message delimiter: LF|CR|NUL. Default is `NUL` to allow embedded `LF` for line string separation.
delim = LF

; Prompt string. Default is none.
prompt = >

; Console color for internal messages. Default is gray.
info_color = green

; Console color for error messages. Default is red.
err_color = red

; Simple user macros that sends text when executed. executed by `ESC name`.
; Quotes can be used to maintain leading or trailing whitespace.
[macros]
dox = "hey server - do something with x"
s3 = "send me a three"

; If the specified text appears in the line, it is colorized.
[matchers]
"abc" = magenta
"xyz" = yellow


# Runtime Commands TODO1 c for clear term

- `ESC q` - quit application
- `ESC h` - show some info
- `ESC <macro>` - execute macro defined in config file

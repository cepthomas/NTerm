# NTerm

Minimalist terminal for simple text-based interfaces like embedded systems.

Supported protocols:
- TCP send line then listen for response line. TODO1? continuous messages.
- UDP server listens to continuous messages. Currently doesn't send.
- Serial port send line then listen for response line. TODO1? continuous messages.

A log file `nterm.log` captures all traffic and internal messages. It is overwritten with each execution
of the application.

# Execution

A standard `.ini` format file is typically used to configure a session:

- `NTerm my_config.ini`

Alternatively, NTerm can run minimally without a config file by one of:

- `NTerm tcp 127.0.0.1 59120`
- `NTerm udp 127.0.0.1 59120`
- `NTerm serial COM1 9600 8N1`


# Configuration File Example


```ini
; Basic config items.
[nterm]

; Protocol flavor - one of:
comm = tcp 127.0.0.1 59120
comm = udp 127.0.0.1 59140
comm = serial COM1 9600 8N1 ; => 6|7|8 bits E|O|N parity 0|1 stop bits

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

; If the specified text appears in the line, it is colorized.
[matchers]
"abc" = magenta
"xyz" = yellow
```

# Runtime Commands

 `ESC q` - quit application
 `ESC h` - show some info
 `ESC <macro>` - execute user macro defined in config file

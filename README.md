# NTerm

Minimalist terminal for simple text-based interfaces like embedded systems.

Supported protocols:
- TCP client sends line to a server then reads one response line.
- UDP listener displays continuous messages. Currently doesn't send.
- Serial port client sends line then listens for one response line.

A log file `%APPDATA%\Ephemera\NTerm\log.txt` captures all traffic and internal messages.

# Execution

A standard `.ini` format file is typically used to configure a session:

- `NTerm my_config.ini`

Alternatively, NTerm can run minimally without a config file by one of:

- `NTerm tcp 127.0.0.1 59120`
- `NTerm udp 127.0.0.1 59120`
- `NTerm serial COM1 9600 8N1`

The default configuration is in `%APPDATA%\Ephemera\NTerm\default.ini`. It is created the first time the
app is run. Edit to your preferences. Any config file loaded from the command line sparsely overlays the defaults.

# Configuration File Format

```ini
; Basic config items.
[nterm]

; Protocol flavor - one of these. Default is `null`.
comm = tcp 127.0.0.1 59120
comm = udp 127.0.0.1 59140
comm = serial COM1 9600 8N1 ; => 6|7|8 bits E|O|N parity 0|1 stop bits

; Message delimiter: LF|CR|NUL. Default is `NUL` to allow embedded `LF` for line string separation.
delim = LF

; Console color for comm messages. Default is yellow.
traffic_color = yellow

; Console color for error messages. Default is red.
error_color = red

; Simple user macros that sends text when executed. executed by `ESC char`.
; Quotes can be used to maintain leading or trailing whitespace.
: char cannot be one of the buitin commands (q, c, h).
[macros]
dox = "hey server - do something with x"
s3 = "send me a three"

; If the specified text appears in the line, it is colorized.
[matchers]
"abc" = magenta
"xyz" = yellow
```

# Runtime Commands

- `ESC q` - quit application
- `ESC c` - clear terminal
- `ESC h` - show some info
- `ESC <macro>` - execute macro defined in config file

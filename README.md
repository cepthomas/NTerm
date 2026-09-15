# NTerm  TODO1 clean up

Minimalist terminal for simple text-based interfaces like embedded systems.

Supported protocols:
- TCP client sends line to a server then reads one response line.
- UDP client displays any received messages. Can send also.
- Serial port client sends line then listens for one response line.

A log file `%APPDATA%\Ephemera\NTerm\log.txt` captures all traffic and internal messages.

# Execution

A standard `.ini` format file is typically used to configure a session:

- `NTerm my_config.ini`

Alternatively, NTerm can run minimally without a config file by one of:

- `NTerm tcp 127.0.0.1 59120`
- `NTerm udp 127.0.0.1 59140`
- `NTerm serial COM1 9600`

The default configuration is in `%APPDATA%\Ephemera\NTerm\default.ini`. It is created the first time the
app is run. Edit to your preferences. Any config file loaded from the command line sparsely overlays the defaults.


# Meta Commands

These meta commands are available in the terminal:
- `<meta_ind>q` - quit application
- `<meta_ind>c` - clear terminal
- `<meta_ind>h` - show some info
- `<meta_ind><macro>` - execute macro defined in config file

# Configuration File Format

```ini
; Basic config items.
[nterm]

; Protocol flavor and config - one of these. Default is `null` which does local loopback.
comm = tcp 127.0.0.1 59120 [delim]
comm = udp 127.0.0.1 59140 [send?]
comm = serial COM1 9600 [framing]
comm = null
; Options:
; delim: message delimiter=NONE|NULL|ESC|LF|CR|CRLF, default is CRLF
; send: make this a sender instead of listener
; framing: bits=6|7|8 parity=E|O|N stop bits=1|2, default is 8N1

; Console color for error messages - optional.
error_color = red

; First char in command indicates a meta command. Default is 'ESC'. 
meta_ind = |

; Simple user macros that sends text when executed using `<meta_ind>name`.
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

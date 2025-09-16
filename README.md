# NTerm

Minimalist terminal for simple text-based interfaces like embedded systems.

Supported protocols:
- TCP line-oriented in a send-then-listen mode or listen to continuous messages.
- UDP listener to continuous messages.
- Serial port like TCP mode.

A log file `nterm.log` captures all traffic and internal messages. It is overwritten with each execution
of the application.

# Configuration

A standard `.ini` format file is used to configure a session.
See `Test\test.ini` for a description of the fields.

Alternatively, NTerm can run without a config file by passing the comm_type field in the cmd line args:

- Tcp: `NTerm tcp 127.0.0.1 59120
- Udp: `NTerm udp 127.0.0.1 59120
- Serial: `NTerm serial serial COM1 9600 8N1`
- Null modem (loopback): `NTerm null`

# Metakeys

System functions and user macros are called by prefacing with the `meta` marker (default shown):

- `!q`: quit
- `!?`: about
- `!uuu`: user macros as defined in `macros` section. 

# Colorized output.

In order to differentiate internally generated messages, `info_color` and `error_color` can be specified.
Additionally simple text matching and coloring is specified in the `matchers` section.

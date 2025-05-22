# NTerm

Minimalist terminal for simple things like embedded systems.

Supports tcp/socket and serial ports.

Very much a work in progress.

## Config

- Name
- CommType: Null, Tcp, Serial
- Args:
  - Tcp: "127.0.0.1", "59120"
  - Serial: "COM1", "9600", "E|O|N", "6|7|8", "0|1|1.5"
- HotKeys: like "k=do something"  "o=send me"
- ... more

## Hotkeys?



## Metakeys?

config metakeychar = '!'

    case "q":
        ts.Cancel();
        break;

    case "s":
        /*var eds =*/
        SettingsEditor.Edit(_settings, "NTerm", 120);
        InitFromSettings();
        break;

    case "?":
        Help();
        break;




# NTerm
Minimalist cli for things like embedded systems.


## Config

- Name
- CommType: Null, Tcp, Serial
- Args:
  - Tcp: "127.0.0.1 59120
  - Seria: "COM1 9600 E|O|N 6|7|8 0|1|1.5\"
- HotKeys: like "k=do something"  "o=send me"

## Keys

// public Keys KeyCode - key code for the event.
// public int KeyValue - integer representation of the KeyCode property. => (int)(KeyData & Keys.KeyCode);
// public Keys KeyData - key code for the key that was pressed, combined with modifier flags
// Alt, Control, Shift


// How windows handles key presses. For example Shift+A produces:
// •   KeyDown: KeyCode=Keys.ShiftKey, KeyData=Keys.ShiftKey | Keys.Shift, Modifiers=Keys.Shift
// •   KeyDown: KeyCode=Keys.A, KeyData=Keys.A | Keys.Shift, Modifiers=Keys.Shift
// •   KeyPress: KeyChar='A'
// •   KeyUp: KeyCode=Keys.A
// •   KeyUp: KeyCode=Keys.ShiftKey
// 
// Also note that Windows steals TAB, RETURN, ESC, and arrow keys so they are not currently implemented.

// Keys.xxx
// D0 = 0x30, // 0
// D9 = 0x39, // 9
// A = 0x41,
// Z = 0x5A,
// Space = 0x20,

// ascii
//65   41   01000001 &#65;     A       UC A
//90   5A   01011010 &#90;     Z       UC Z
//97   61   01100001 &#97;     a       LC a
//122  7A   01111010 &#122;    z       LC z


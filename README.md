# NTerm
Minimalist cli for things like embedded systems. Supports tcp/socket and serial ports.




// ArgumentException   Raised when a non-null argument that is passed to a method is invalid.
// ArgumentNullException   Raised when null argument is passed to a method.
// ArgumentOutOfRangeException Raised when the value of an argument is outside the range of valid values.
// DivideByZeroException   Raised when an integer value is divide by zero.
// InvalidOperationException   Raised when a method call is invalid in an object's current state.
// NotSupportedException   Raised when a method or operation is not supported.
// TimeoutException    The time interval allotted to an operation has expired.


//>>>>>> https://www.reddit.com/r/csharp/comments/1aga16j/correct_creation_of_long_running_tasks/

//>>>>>> https://learn.microsoft.com/en-us/dotnet/core/extensions/channels
// https://gist.github.com/RupertAvery/adff0e177fdbb096670a2022ec12d957



AliceBlue = 28, AntiqueWhite = 29, Aqua = 30, Aquamarine = 31, Azure = 32, Beige = 33, Bisque = 34, Black = 35, BlanchedAlmond = 36, Blue = 37, BlueViolet = 38, Brown = 39, BurlyWood = 40, CadetBlue = 41, Chartreuse = 42, Chocolate = 43, Coral = 44, CornflowerBlue = 45, Cornsilk = 46, Crimson = 47, Cyan = 48, DarkBlue = 49, DarkCyan = 50, DarkGoldenrod = 51, DarkGray = 52, DarkGreen = 53, DarkKhaki = 54, DarkMagenta = 55, DarkOliveGreen = 56, DarkOrange = 57, DarkOrchid = 58, DarkRed = 59, DarkSalmon = 60, DarkSeaGreen = 61, DarkSlateBlue = 62, DarkSlateGray = 63, DarkTurquoise = 64, DarkViolet = 65, DeepPink = 66, DeepSkyBlue = 67, DimGray = 68, DodgerBlue = 69, Firebrick = 70, FloralWhite = 71, ForestGreen = 72, Fuchsia = 73, Gainsboro = 74, GhostWhite = 75, Gold = 76, Goldenrod = 77, Gray = 78, Green = 79, GreenYellow = 80, Honeydew = 81, HotPink = 82, IndianRed = 83, Indigo = 84, Ivory = 85, Khaki = 86, Lavender = 87, LavenderBlush = 88, LawnGreen = 89, LemonChiffon = 90, LightBlue = 91, LightCoral = 92, LightCyan = 93, LightGoldenrodYellow = 94, LightGray = 95, LightGreen = 96, LightPink = 97, LightSalmon = 98, LightSeaGreen = 99, LightSkyBlue = 100, LightSlateGray = 101, LightSteelBlue = 102, LightYellow = 103, Lime = 104, LimeGreen = 105, Linen = 106, Magenta = 107, Maroon = 108, MediumAquamarine = 109, MediumBlue = 110, MediumOrchid = 111, MediumPurple = 112, MediumSeaGreen = 113, MediumSlateBlue = 114, MediumSpringGreen = 115, MediumTurquoise = 116, MediumVioletRed = 117, MidnightBlue = 118, MintCream = 119, MistyRose = 120, Moccasin = 121, NavajoWhite = 122, Navy = 123, OldLace = 124, Olive = 125, OliveDrab = 126, Orange = 127, OrangeRed = 128, Orchid = 129, PaleGoldenrod = 130, PaleGreen = 131, PaleTurquoise = 132, PaleVioletRed = 133, PapayaWhip = 134, PeachPuff = 135, Peru = 136, Pink = 137, Plum = 138, PowderBlue = 139, Purple = 140, RebeccaPurple = 175 Red = 141, RosyBrown = 142, RoyalBlue = 143, SaddleBrown = 144, Salmon = 145, SandyBrown = 146, SeaGreen = 147, SeaShell = 148, Sienna = 149, Silver = 150, SkyBlue = 151, SlateBlue = 152, SlateGray = 153, Snow = 154, SpringGreen = 155, SteelBlue = 156, Tan = 157, Teal = 158, Thistle = 159, Tomato = 160, Turquoise = 161, Violet = 162, Wheat = 163, White = 164, WhiteSmoke = 165, Yellow = 166, YellowGreen = 167,  

               

## Config

- Name
- CommType: Null, Tcp, Serial
- Args:
  - Tcp: "127.0.0.1", "59120"
  - Serial: "COM1", "9600", "E|O|N", "6|7|8", "0|1|1.5"
- HotKeys: like "k=do something"  "o=send me"
- ... more


## Keys

KeyEventArgs:
- public Keys KeyCode - key code for the event.
- public int KeyValue - integer representation of the KeyCode property. => (int)(KeyData & Keys.KeyCode);
- public Keys KeyData - key code for the key that was pressed, combined with modifier flags
- Alt, Control, Shift

Keys.xxx:
- D0 = 0x30 [0]
- D9 = 0x39 [9]
- A = 0x41
- Z = 0x5A
- Space = 0x20

No UC/LC variants like ascii - need to examine the Shift flag.


How windows handles key presses. For example Shift+A produces:
- KeyDown: KeyCode=Keys.ShiftKey, KeyData=Keys.ShiftKey | Keys.Shift, Modifiers=Keys.Shift
- KeyDown: KeyCode=Keys.A, KeyData=Keys.A | Keys.Shift, Modifiers=Keys.Shift
- KeyPress: KeyChar='A'
- KeyUp: KeyCode=Keys.A
- KeyUp: KeyCode=Keys.ShiftKey

Note:
- KeyPress converts to ascii and is not executed for non-ascii inputs e.g. Fkeys.
- Windows steals TAB, RETURN, ESC, and arrow keys so they are not currently implemented.


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


## Config

- Name
- CommType: Null, Tcp, Serial
- Args:
  - Tcp: "127.0.0.1", "59120"
  - Serial: "COM1", "9600", "E|O|N", "6|7|8", "0|1|1.5"
- HotKeys: like "k=do something"  "o=send me"
- ... more

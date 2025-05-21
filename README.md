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


# Stuff

## Full match coloring

``` C#
[DisplayName("Whole Word")]
[Description("Match whole word")]
[Browsable(true)]
public bool WholeWord { get; set; } = false;

[DisplayName("Whole Line")]
[Description("Color whole line or just word")]
[Browsable(true)]
public bool WholeLine { get; set; } = true;


// Check for text match.
bool hasMatch = false;
foreach (Matcher m in _config.Matchers)
{
    int pos = 0;

    do
    {
        pos = text.IndexOf(m.Text, pos);
        if (pos >= 0)
        {
            if (m.WholeWord)
            {
                // Check neighbors.
                bool leftww = pos == 0 || !char.IsAsciiLetterOrDigit(text[pos - 1]);
                bool rightww = pos + m.Text.Length >= text.Length || !char.IsAsciiLetterOrDigit(text[pos + 1]);

                if (leftww && rightww)
                {
                    DoOneMatch(m);
                }
                else
                {
                    Console.Write(m.Text);
                }
            }
            else
            {
                DoOneMatch(m);
            }
        }
    }
    while (pos >= 0);
}

if (nl)
{
    Console.Write(Environment.NewLine);
}

// Local function.
static void DoOneMatch(Matcher m)
{
    if (m.ForeColor is not ConsoleColorEx.None) { Console.ForegroundColor = (ConsoleColor)m.ForeColor; }
    if (m.BackColor is not ConsoleColorEx.None) { Console.BackgroundColor = (ConsoleColor)m.BackColor; }
    Console.Write(m.Text);
    Console.ResetColor();
}
```


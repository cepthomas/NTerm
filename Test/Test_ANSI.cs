using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;


namespace NTerm.Test
{
    public class ANSI_COLOR : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Tests ANSI color functions.");

            // ESC = \033
            // One of: ESC[IDm  ESC[38;5;IDm  ESC[48;5;IDm  ESC[38;2;R;G;Bm  ESC[48;2;R;G;Bm

            var (color, invert) = Ansi.ColorFromAnsi("bad string");
            UT_TRUE(color.IsEmpty);

            (color, invert) = Ansi.ColorFromAnsi("\033[34m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ff00007f");

            (color, invert) = Ansi.ColorFromAnsi("\033[45m");
            UT_TRUE(invert);
            UT_EQUAL(color.Name, "ff7f007f");

            // system
            (color, invert) = Ansi.ColorFromAnsi("\033[38;5;12m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ff0000ff");

            // id
            (color, invert) = Ansi.ColorFromAnsi("\033[38;5;122m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ff87ffd7");

            // grey
            (color, invert) = Ansi.ColorFromAnsi("\033[38;5;249m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ffb2b2b2");

            // id bg
            (color, invert) = Ansi.ColorFromAnsi("\033[48;5;231m");
            UT_TRUE(invert);
            UT_EQUAL(color.Name, "ffffffff");


            //ESC[38;2;R;G;Bm
            // rgb
            (color, invert) = Ansi.ColorFromAnsi("\033[38;2;204;39;187m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ffcc27bb");

            // rgb invert
            (color, invert) = Ansi.ColorFromAnsi("\033[48;2;19;0;222m");
            UT_TRUE(invert);
            UT_EQUAL(color.Name, "ff1300de");
        }
    }



    class RegexTests
    {
        void Write(string s)
        {

        }

        void TestRegex()
        {

            // https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.group.captures


            /* >>>
            Matched:0-14
                Group0:No color [31m
                  Capture0:No color [31m
                Group1:No color 
                  Capture0:No color 
                Group2:31
                  Capture0:31
            Matched:14-27
                Group0: Standard color [38;5;32m
                  Capture0: Standard color [38;5;32m
                Group1: Standard color
                  Capture0: Standard color
                Group2:38;5;32
                  Capture0:38;5;32
            Matched:41-31
                Group0:  256 Color [38;2;60;120;180m
                  Capture0:  256 Color [38;2;60;120;180m
                Group1:  256 Color 
                  Capture0:  256 Color
                Group2:38;2;60;120;180
                  Capture0:38;2;60;120;180
            Matched:72-17
                Group0:  RGB Color [0m
                  Capture0:  RGB Color [0m
                Group1:  RGB Color
                  Capture0:  RGB Color
                Group2:0
                  Capture0:0
            Dangling: reset
            */

            string _ansiPattern = @"([^\u001B]+)\u001B\[([^m)]+)m";
            string s = "No color [31m Standard colors [38;5;32m  256 Colors [38;2;60;120;180m  RGB Colors [0m reset";

            int end = 0;
            var matches = Regex.Matches(s, _ansiPattern);
            foreach (Match match in Regex.Matches(s, _ansiPattern))
            {
                end = match.Index + match.Length;

                //Write($"Matched text: {match.Value} [{match.Index} {match.Length}]");
                Write($"Matched:{match.Index}-{match.Length}");

                int groupCtr = 0;
                foreach (Group group in match.Groups)
                {
                    Write($"  Group{groupCtr}:{group.Value}");
                    groupCtr++;

                    int capCtr = 0;
                    foreach (Capture capture in group.Captures)
                    {
                        Write($"    Capture{capCtr}:{capture.Value}");
                        capCtr++;
                    }
                }
            }

            var dangling = s.Substring(end);
            Write($"Dangling:{dangling}");
        }


        void RegexLeftovers() // TODO 
        {
            /*
            \x1b[34;01m
            "\u001B"
            [38;5;32m
            (\u001B\[\d+;\d+m)+

            section-heading:
            - match: ^(#+ +[^\[]+) *(?:\[(.*)\])?\n

            link definition <name>(link)
            - match: <([^>)]+)>\(([^\)]+)\)
            captures:
            1: markup.link.name.notr
            2: markup.link.target.notr
            3: markup.link.tags.notr
            */

// https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.group.captures

            string pattern = @"([^\u001B]+)\u001B\[([^m)]+)m";
            string input = "No color [31m Standard colors [38;5;32m  256 Colors [38;2;60;120;180m  RGB Colors [0m reset";
            //>>>
            //Matched text: No color [31m [0 14]
            //  Group 0: No color [31m
            //    Capture 1 :  No color [31m
            //  Group 1: No color 
            //    Capture 2 :  No color 
            //  Group 2: 31
            //    Capture 3 :  31
            //Matched text:  Standard colors [38;5;32m [14 27]
            //  Group 0:  Standard colors [38;5;32m
            //    Capture 1 :   Standard colors [38;5;32m
            //  Group 1:  Standard colors 
            //    Capture 2 :   Standard colors 
            //  Group 2: 38;5;32
            //    Capture 3 :  38;5;32
            //Matched text:   256 Colors [38;2;60;120;180m [41 31]
            //  Group 0:   256 Colors [38;2;60;120;180m
            //    Capture 1 :    256 Colors [38;2;60;120;180m
            //  Group 1:   256 Colors 
            //    Capture 2 :    256 Colors 
            //  Group 2: 38;2;60;120;180
            //    Capture 3 :  38;2;60;120;180
            //Matched text:   RGB Colors [0m [72 17]
            //  Group 0:   RGB Colors [0m
            //    Capture 1 :    RGB Colors [0m
            //  Group 1:   RGB Colors 
            //    Capture 2 :    RGB Colors 
            //  Group 2: 0
            //    Capture 3 :  0


            bool mult = true;
            if (mult)
            {
                int lastIndex = 0;

                var matches = Regex.Matches(input, pattern);

                foreach (Match match in Regex.Matches(input, pattern))
                {
                    lastIndex = match.Index + match.Length;

                    Write($"Matched text: {match.Value} [{match.Index} {match.Length}]");

                    int groupCtr = 0;

                    foreach (Group group in match.Groups)
                    {
                        Write($"  Group {groupCtr}: {group.Value}");
                        groupCtr++;

                        int capCtr = 0;
                        foreach (Capture capture in group.Captures)
                        {
                            Write($"    Capture {groupCtr} :  {group.Value}");
                            capCtr++;
                        }
                    }
                }

                var dangling = input.Substring(lastIndex);
                Write($"  Dangling : [{dangling}]");
            }


            //string pattern = @"([^<]+)<([^>)]+)>\(([^\)]+)\)";
            //string input = "1111 <name1>(lnk1)  2222 <name2>(lnk2)  3333 <name3>(lnk3)";
            //>>>
            //Matched text: 1111 <name1>(lnk1)
            //  Group 1: 1111 
            //    Capture 0: 1111 
            //  Group 2: name1
            //    Capture 0: name1
            //  Group 3: lnk1
            //    Capture 0: lnk1
            //Matched text:   2222 <name2>(lnk2)
            //  Group 1:   2222 
            //    Capture 0:   2222 
            //  Group 2: name2
            //    Capture 0: name2
            //  Group 3: lnk2
            //    Capture 0: lnk2


            //string pattern = @"\b(\w+\s*)+\.";
            //string input = "This is a sentence. This is another sentence.";
            //>>>
            //Matched text: This is a sentence.
            //  Group 1: sentence
            //    Capture 0: This 
            //    Capture 1: is 
            //    Capture 2: a 
            //    Capture 3: sentence
            //Matched text: This is another sentence.
            //  Group 1: sentence
            //    Capture 0: This 
            //    Capture 1: is 
            //    Capture 2: another 
            //    Capture 3: sentence

            // 1st Capturing Group (\w+\s*)+
            //   + matches the previous token between one and unlimited times, as many times as possible, giving back as needed (greedy)
            //   \w match any word character in any script (equivalent to [\p{L}\p{Mn}\p{Nd}\p{Pc}])
            //   + matches the previous token between one and unlimited times, as many times as possible, giving back as needed (greedy)
            //   \s matches any kind of invisible character (equivalent to [\f\n\r\t\v\p{Z}])
            //   * matches the previous token between zero and unlimited times, as many times as possible, giving back as needed (greedy)
            // \. matches the character . with index 4610 (2E16 or 568) literally (case sensitive)


            //string input = "QSMDRYCELL   11.00   11.10   11.00   11.00    -.90      11     11000     1.212";
            //string pattern = @"^(\S+)\s+(\s+[\d.-]+){8}$";
            // >>>
            //Matched text: QSMDRYCELL   11.00   11.10   11.00   11.00 - .90      11     11000     1.212
            //   Group 1: QSMDRYCELL
            //      Capture 0: QSMDRYCELL
            //   Group 2:      1.212
            //      Capture 0:  11.00
            //      Capture 1:    11.10
            //      Capture 2:    11.00
            //      Capture 3:    11.00
            //      Capture 4:     -.90
            //      Capture 5:       11
            //      Capture 6:      11000
            //      Capture 7:      1.212


            // string pattern = @"\b(\w+)\b";
            // string input = "This is one sentence.";
            // >>>
            //       Matched text: This
            //          Group 1:  This
            //             Capture 0: This


            //// extract and clean up port name and number
            //c.name = p.GetPropertyValue("Name").ToString();
            //Match mName = Regex.Match(c.name, namePattern);
            //if (mName.Success)
            //{
            //    c.name = mName.Value;
            //    c.num = int.Parse(c.name.Substring(3));
            //}

            //// if the port name or number cannot be determined, skip this port and move on
            //if (c.num < 1)
            //{
            //    continue;
            //}

            //// get the device's VID and PID
            //string pidvid = p.GetPropertyValue("PNPDeviceID").ToString();

            //// extract and clean up device's VID
            //Match mVID = Regex.Match(pidvid, vidPattern, RegexOptions.IgnoreCase);
            //if (mVID.Success)
            //{
            //    c.vid = mVID.Groups[1].Value.Substring(0, Math.Min(4, c.vid.Length));
            //}
        }
    }
}

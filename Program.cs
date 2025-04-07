using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

#if NET35_OR_GREATER || NETCOREAPP
using System.Linq;
using System.Linq.Expressions;

#endif
#if NET45_OR_GREATER || NETCOREAPP
using System.Threading.Tasks;
#endif

class MatrixScreenSaver
{
    static Dictionary<int, Column> columns = new Dictionary<int, Column>();
    static Random rand = new Random();
    static bool isRunning = true;
    static int lastWidth = -1;
    static int lastHeight = -1;
    static CharTypeEnum charType = CharTypeEnum.bin;

    private enum CharTypeEnum
    {
        ascii,
        japanese,
        hex,
        bin
    }

#if NET45_OR_GREATER || NETCOREAPP
static async Task Main(string[] args)
#else
    static void Main(string[] args)
#endif
    {
        // Set the console output encoding to UTF-8
        Console.OutputEncoding = Encoding.UTF8;

        if(args.Length == 0)
        {
            Console.WriteLine("Usage: FSMAtrix [ascii|bin|hex|japanese]");
            return;
        }

        if (args[args.Length - 1].ToLower() == "japanese")
        {
            charType = CharTypeEnum.japanese;
        }
        else if (args[args.Length - 1].ToLower() == "hex")
        {
            charType = CharTypeEnum.hex;
        }
        else if (args[args.Length - 1].ToLower() == "ascii")
        {
            charType = CharTypeEnum.ascii;
        }
        else
        {
            charType = CharTypeEnum.bin;
        }


        // Initializing the screen saver
#if NET45_OR_GREATER || NETCOREAPP
        await StartCMatrix();
#else
        StartCMatrix();
#endif

        // Cleanup and reset
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Clear();
    }

#if NET45_OR_GREATER || NETCOREAPP
    static async Task StartCMatrix(int maxColumns = 64, int frameWait = 100)
#else
    static void StartCMatrix(int maxColumns = 64, int frameWait = 100)
#endif
    {
        while (isRunning)
        {
            if (lastHeight != Console.WindowHeight || lastWidth != Console.WindowWidth)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();

                lastHeight = Console.WindowHeight;
                lastWidth = Console.WindowWidth;
            }

            WriteFrameBuffer(maxColumns);
            ShowFrameBuffer();
#if NET45_OR_GREATER || NETCOREAPP
            await Task.Delay(frameWait);
#else
            Thread.Sleep(frameWait);
#endif
            // Check for user input to exit
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    isRunning = false;
                }
            }
        }
    }

    static void WriteFrameBuffer(int maxColumns)
    {
        if (columns.Count < maxColumns)
        {
            // 50% chance to add a new column
            if (rand.NextDouble() < 0.5)
            {
                int x;
                do
                {
                    x = rand.Next(0, Console.WindowWidth);
                } while (columns.ContainsKey(x));

                columns.Add(x, new Column(x));
            }
        }
    }

    static void ShowFrameBuffer()
    {
        var completed = new List<int>();

        foreach (var entry in columns)
        {
            if (!entry.Value.Step())
            {
                completed.Add(entry.Key);
            }
        }

        foreach (var key in completed)
        {
            columns.Remove(key);
        }
    }

    class Column
    {
        public int XPos { get; set; }
        public int YLimit { get; set; }
        public int Head { get; set; }
        public int Fade { get; set; }
        public int FadeLength { get; set; }

        public Column(int xPos)
        {
            XPos = xPos;
            YLimit = Console.WindowHeight;
            Head = 1;
            Fade = 0;
            FadeLength = (int)(Math.Abs(YLimit / 3.0) * (1 + rand.Next(-30, 50) / 100.0));

            // Randomize fade length
            FadeLength += rand.Next(0, FadeLength);
        }

        public bool Step()
        {
            if (Head < YLimit)
            {
                SetBufferCell(XPos, Head, GetRandomCharacter(charType), ConsoleColor.White);
                SetBufferCell(XPos, Head - 1, GetRandomCharacter(charType), ConsoleColor.Green);
                Head++;
            }

            if (Head > FadeLength)
            {
                SetBufferCell(XPos, Fade, GetRandomCharacter(charType), ConsoleColor.DarkGreen);

                // Tail end
                int tail = Fade - 1;
                if (tail < YLimit)
                {
                    SetBufferCell(XPos, Fade - 1, ' ', ConsoleColor.Black);
                }

                Fade++;
            }

            if (Fade < YLimit)
            {
                return true;
            }

            // Clean-up the last row of the tail
            if ((Fade - 1) < YLimit)
            {
                SetBufferCell(XPos, Fade - 1, ' ', ConsoleColor.Black);
            }

            return false;
        }

        void SetBufferCell(int x, int y, char character, ConsoleColor color)
        {
            if (x >= 0 && x < (Console.WindowWidth - 1) && y >= 0 && y < Console.WindowHeight)
            {
                Console.SetCursorPosition(x, y);
                Console.ForegroundColor = color;
                Console.Write(character);
            }
        }

        // Generate a random character
        char GetRandomCharacter(CharTypeEnum charType)
        {
            switch(charType)
            {
                case CharTypeEnum.ascii:
                    string ascii = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!·$%&/()=?¿*-:;ºª";
                    return ascii[rand.Next(ascii.Length)];
                case CharTypeEnum.japanese:
                    int choice = rand.Next(3);

                    if (choice == 0)
                    {
                        // Hiragana: primeros 25 caracteres más comunes
                        string hiragana = "あいうえおかきくけこさしすせそたちつてとなにぬねの";
                        return hiragana[rand.Next(hiragana.Length)];
                    }
                    else if (choice == 1)
                    {
                        // Katakana: primeros 25 caracteres más comunes
                        string katakana = "アイウエオカキクケコサシスセソタチツテトナニヌネノ";
                        return katakana[rand.Next(katakana.Length)];
                    }
                    else
                    {
                        // Kanji: caracteres comunes
                        string kanji = "一二三四五六七八九十百千万円口目手足早長明正高中大";
                        return kanji[rand.Next(kanji.Length)];
                    }
                case CharTypeEnum.hex:
                    string hexadecimal = "1234567890ABCDEF";
                    return hexadecimal[rand.Next(hexadecimal.Length)];
                case CharTypeEnum.bin:
                    string binary = "01";
                    return binary[rand.Next(binary.Length)];
                default:
                    return '0';
            }
        }
    }

    // Helper to simulate the screen saver timer functionality
    static void RegisterTimer()
    {
        Timer timer = new Timer((e) =>
        {
            if (!isRunning)
            {
#if NET45_OR_GREATER || NETCOREAPP
                StartCMatrix().Wait();
#else
                StartCMatrix();
#endif
            }
        }, null, 0, 1000);
    }
}

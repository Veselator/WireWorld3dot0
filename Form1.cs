using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text;

namespace WireWorld3dot0
{
    public static class RichTextBoxExtensions
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETREDRAW = 0x000B;

        public static void BeginUpdate(this RichTextBox rtb)
        {
            SendMessage(rtb.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        public static void EndUpdate(this RichTextBox rtb)
        {
            SendMessage(rtb.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            rtb.Invalidate();
        }
    }

    public partial class Form1 : Form
    {
        private const string PROMPT = "> ";
        private static readonly Regex SET_RELATIVE_PATTERN = new Regex(@"^set\s+(-?\d+)\s+(-?\d+)\s+(\S+)\s+(\S+)$", RegexOptions.Compiled);
        private static readonly Regex SET_PATTERN = new Regex(@"^set\s+(\S+)\s+(\S+)$", RegexOptions.Compiled);
        private static readonly Regex SET_AREA_PATTERN = new Regex(@"^set\s+(-?\d+)\s+(-?\d+)\s+(-?\d+)\s+(-?\d+)\s+(\S+)\s+(\S+)$", RegexOptions.Compiled);
        private static readonly Regex SET_GAME_VARIABLE_PATTERN = new Regex(@"^setvar\s+(\S+)\s+(\d+)$", RegexOptions.Compiled);
        private static readonly Regex SET_EMPTY_PATTERN = new Regex(@"^set empty$", RegexOptions.Compiled);
        private static readonly Regex CLEAR_PATTERN = new Regex(@"^clear$", RegexOptions.Compiled);
        private static readonly Regex SAVE_PATTERN = new Regex(@"^save\s+(\S+)$", RegexOptions.Compiled);
        private static readonly Regex LOAD_PATTERN = new Regex(@"^load\s+(\S+)$", RegexOptions.Compiled);
        private static readonly Regex TIME_PATTERN = new Regex(@"^time\s+(\S+)$", RegexOptions.Compiled);

        private bool isRunning;
        bool isPaused;
        private System.Windows.Forms.Timer gameTimer;
        private System.Windows.Forms.Timer logicTimer;
        private string currentInput = string.Empty;

        // Для отслеживания состояния клавиш
        private Dictionary<Keys, bool> keyState = new Dictionary<Keys, bool>();
        private HashSet<Keys> pressedKeys = new HashSet<Keys>();
        private HashSet<Keys> processedKeys = new HashSet<Keys>();

        RichTextBox outputConsole = new RichTextBox();
        RichTextBox inputConsole = new RichTextBox();

        static int outputConsoleHeight = 90;
        private readonly int[] borderSize = { 20, 20 };
        private readonly float[] charSize = { 16.2f, 17f };

        Screen gameScreen;

        TileMatrix gameMatrix;// = new TileMatrix(70, 70);
        TileMatrix bufferMatrix;// = new TileMatrix(70, 70);

        private int _timePerTick = 50;
        private int _timePerLogicTick = 1000;
        public int timePerTick
        {
            get { return _timePerTick; }
            set
            {
                _timePerTick = value;
                gameTimer.Interval = _timePerTick;
            }
        }

        public int timePerLogicTick
        {
            get { return _timePerLogicTick; }
            set
            {
                _timePerLogicTick = value;
                logicTimer.Interval = _timePerLogicTick;
            }
        }

        private Dictionary<String, TileDirection> string2Directions = new Dictionary<String, TileDirection>();
        private Dictionary<String, TileType> string2Type = new Dictionary<String, TileType>();

        private readonly Color inputConsoleColor = Color.Lime;
        private readonly Color outputConsoleColor = Color.White;

        private readonly StringBuilder outputContentBuilder = new StringBuilder(1000);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LogManager.addNote("Початок завантаження форми");

            gameMatrix = new TileMatrix(70, 70);
            bufferMatrix = new TileMatrix(gameMatrix);

            InitializeDictionaries();
            SetupOutputConsole();
            SetupInputConsole();
            AttachEventHandlers();
            SetupTimers();

            LogManager.addNote("Успішне завантаження форми");
            StartMainLoop();
        }

        private void InitializeDictionaries()
        {
            string2Directions.Add("top", TileDirection.Top);
            string2Directions.Add("left", TileDirection.Left);
            string2Directions.Add("bottom", TileDirection.Bottom);
            string2Directions.Add("right", TileDirection.Right);

            string2Type.Add("arrow", TileType.Arrow);
            string2Type.Add("empty", TileType.Empty);
            string2Type.Add("generator", TileType.Generator);
            string2Type.Add("no", TileType.No);
            string2Type.Add("or", TileType.Or);
            string2Type.Add("and", TileType.And);
            string2Type.Add("equals", TileType.Equals);
            string2Type.Add("xor", TileType.Xor);
            string2Type.Add("take", TileType.Take);
            string2Type.Add("hold", TileType.Hold);
            string2Type.Add("double", TileType.DoubleArrow);
        }

        private void SetupOutputConsole()
        {
            outputConsole.Top = borderSize[1];
            outputConsole.Left = borderSize[0];
            outputConsole.Size = new Size(this.Size.Width - 3 * borderSize[0], this.Size.Height - outputConsoleHeight - 2 * borderSize[1]);
            gameScreen = new Screen(Convert.ToInt32(outputConsole.Width / charSize[0]),
                Convert.ToInt32(outputConsole.Height / charSize[1]));

            outputConsole.BackColor = Color.Black;
            outputConsole.ForeColor = outputConsoleColor;
            outputConsole.BorderStyle = BorderStyle.None;
            outputConsole.ScrollBars = RichTextBoxScrollBars.None;
            outputConsole.WordWrap = false;

            try
            {
                using (PrivateFontCollection privateFonts = new PrivateFontCollection())
                {
                    string fontPath = Path.Combine(Application.StartupPath, "WireworldGraphics.ttf");
                    privateFonts.AddFontFile(fontPath);
                    Font customFont = new Font(privateFonts.Families[0], 12);
                    outputConsole.Font = customFont;
                }
            }
            catch (Exception ex)
            {
                outputConsole.Font = new Font("Consolas", 12);
                MessageBox.Show($"Помилка шрифта: {ex.Message}");
            }

            // ВАЖЛИВО!
            outputConsole.ReadOnly = true;
            outputConsole.Enabled = true;
            outputConsole.TabStop = false;
            outputConsole.Cursor = Cursors.Arrow;
            outputConsole.SelectionLength = 0;
            outputConsole.SelectionProtected = true;

            this.Controls.Add(outputConsole);
        }

        private void SetupInputConsole()
        {
            inputConsole.Size = new Size(this.Size.Width - 3 * borderSize[0], outputConsoleHeight - 3 * borderSize[1]);
            inputConsole.Top = this.Size.Height - outputConsoleHeight;
            inputConsole.Left = borderSize[0];

            inputConsole.BackColor = Color.Black;
            inputConsole.ForeColor = inputConsoleColor;
            inputConsole.BorderStyle = BorderStyle.None;
            inputConsole.ScrollBars = RichTextBoxScrollBars.None;
            inputConsole.WordWrap = false;
            inputConsole.Font = new Font("Consolas", 14);

            this.Controls.Add(inputConsole);
        }

        private void AttachEventHandlers()
        {
            outputConsole.KeyDown += (sender, e) => { e.Handled = true; };
            outputConsole.KeyUp += (sender, e) => { e.Handled = true; };
            outputConsole.GotFocus += (sender, e) => { inputConsole.Focus(); };
            outputConsole.MouseClick += (sender, e) => { inputConsole.Focus(); };
            outputConsole.MouseDown += (sender, e) => { inputConsole.Focus(); };

            inputConsole.KeyDown += (sender, e) => {
                e.Handled = true;
                CheckInput(sender, e);
            };

            inputConsole.MouseDown += (sender, e) => {
                inputConsole.SelectionStart = inputConsole.Text.Length;
                inputConsole.SelectionLength = 0;
            };

            inputConsole.MouseUp += (sender, e) => {
                inputConsole.SelectionStart = inputConsole.Text.Length;
                inputConsole.SelectionLength = 0;
            };

            this.KeyDown += (sender, e) => { e.Handled = true; };
            this.KeyUp += (sender, e) => { e.Handled = true; };
            this.Resize += Form1_Resize;
            this.FormClosing += StopMainLoop;
        }

        private void SetupTimers()
        {
            // Таймер главного цикла
            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = _timePerTick;
            gameTimer.Tick += Tick;

            // Таймер логики
            logicTimer = new System.Windows.Forms.Timer();
            logicTimer.Interval = _timePerLogicTick;
            logicTimer.Tick += Logic;
        }

        private void StartMainLoop()
        {
            LogManager.addNote("Початок головного циклу");
            ShowPrompt();

            isRunning = true;
            gameTimer.Start();
            logicTimer.Start();
        }

        private void StopMainLoop()
        {
            LogManager.addNote("Закінчення головного циклу");
            LogManager.printLogs();
            isRunning = false;
            gameTimer.Stop();
            logicTimer.Stop();
        }

        private void StopMainLoop(object sender, EventArgs e)
        {
            StopMainLoop();
        }

        private void Tick(object sender, EventArgs e)
        {
            if (!isRunning) return;

            Render();
        }

        private bool IsArrow(Keys key)
        {
            return key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right;
        }

        private void CheckInput(object sender, KeyEventArgs e)
        {
            Keys key = e.KeyCode;
            LogManager.addNote($"Key is {key}");
            if (key == Keys.Enter)
            {
                ProcessInput(currentInput);
                currentInput = string.Empty;
                inputConsole.BeginUpdate();
                inputConsole.Clear();
                ShowPrompt();
                inputConsole.EndUpdate();
            }
            else if (key == Keys.Back)
            {
                if (currentInput.Length > 0)
                {
                    currentInput = currentInput.Substring(0, currentInput.Length - 1);
                    inputConsole.BeginUpdate();
                    inputConsole.Clear();
                    Write(PROMPT + currentInput, inputConsoleColor, inputConsole);
                    inputConsole.EndUpdate();
                }
            }
            if (key == Keys.Right && gameScreen.position.X < gameMatrix.width - 1)
            {
                gameScreen.Move(1, 0);
            }
            else if (key == Keys.Left && gameScreen.position.X > 0)
            {
                gameScreen.Move(-1, 0);
            }
            else if (key == Keys.Up && gameScreen.position.Y > 0)
            {
                gameScreen.Move(0, -1);
            }
            else if (key == Keys.Down && gameScreen.position.Y < gameMatrix.height - 1)
            {
                gameScreen.Move(0, 1);
            }

            else if (IsInputKey(key))
            {
                char c = KeyToChar(key);
                if (c != '\0')
                {
                    currentInput += c;
                    Write(c, inputConsoleColor, inputConsole);
                }
            }
        }

        // Преобразование кода клавиши в символ
        private char KeyToChar(Keys key)
        {
            bool shift = keyState.ContainsKey(Keys.ShiftKey) && keyState[Keys.ShiftKey];

            // Буквы
            if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)('a' + (key - Keys.A));
                if (shift) c = char.ToUpper(c);
                return c;
            }

            // Цифры на основной клавиатуре
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                if (!shift) return (char)('0' + (key - Keys.D0));

                // Символы над цифрами при нажатом Shift
                switch (key)
                {
                    case Keys.D1: return '!';
                    case Keys.D2: return '@';
                    case Keys.D3: return '#';
                    case Keys.D4: return '$';
                    case Keys.D5: return '%';
                    case Keys.D6: return '^';
                    case Keys.D7: return '&';
                    case Keys.D8: return '*';
                    case Keys.D9: return '(';
                    case Keys.D0: return ')';
                }
            }

            // Цифры на доп. клавиатуре
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return (char)('0' + (key - Keys.NumPad0));

            // Пробел
            if (key == Keys.Space)
                return ' ';

            // Спец. символы
            switch (key)
            {
                case Keys.OemPeriod: return shift ? '>' : '.';
                case Keys.Oemcomma: return shift ? '<' : ',';
                case Keys.OemQuestion: return shift ? '?' : '/';
                case Keys.OemSemicolon: return shift ? ':' : ';';
                case Keys.OemQuotes: return shift ? '"' : '\'';
                case Keys.OemOpenBrackets: return shift ? '{' : '[';
                case Keys.OemCloseBrackets: return shift ? '}' : ']';
                case Keys.OemPipe: return shift ? '|' : '\\';
                case Keys.OemMinus: return shift ? '_' : '-';
                case Keys.Oemplus: return shift ? '+' : '=';
                case Keys.Oemtilde: return shift ? '~' : '`';
            }

            return '\0';
        }

        // Проверка, является ли клавиша "вводной"
        private bool IsInputKey(Keys key)
        {
            return (key >= Keys.A && key <= Keys.Z) ||
                   (key >= Keys.D0 && key <= Keys.D9) ||
                   (key >= Keys.NumPad0 && key <= Keys.NumPad9) ||
                   key == Keys.Space ||
                   key == Keys.OemPeriod ||
                   key == Keys.Oemcomma ||
                   key == Keys.OemQuestion ||
                   key == Keys.OemSemicolon ||
                   key == Keys.OemQuotes ||
                   key == Keys.OemOpenBrackets ||
                   key == Keys.OemCloseBrackets ||
                   key == Keys.OemPipe ||
                   key == Keys.OemMinus ||
                   key == Keys.Oemplus ||
                   key == Keys.Oemtilde;
        }

        private void Logic(object sender, EventArgs e)
        {
            if (isPaused) return;
            LogManager.addNote("Обробляю нову ітерацію");
            bufferMatrix.CopyFrom(gameMatrix); // Копіюємо матрицю
            bufferMatrix.tick();
            gameMatrix.CopyFrom(bufferMatrix); // Обратно копіюємо
        }

        private void Render()
        {
            outputContentBuilder.Clear();
            DrawHeader();
            DrawContent(outputContentBuilder);

            outputConsole.BeginUpdate();
            outputConsole.Clear();
            Write(outputContentBuilder.ToString(), outputConsoleColor, outputConsole);
            outputConsole.EndUpdate();
        }

        // Заголовок
        private void DrawHeader()
        {
            this.Text = $"WireWorld 3.0: тік = {timePerTick} мс     тік логіки = {timePerLogicTick} мс     {gameScreen.position}     {gameMatrix.getTypeOfTile(gameScreen.position)}";
        }

        private void DrawContent(StringBuilder outputContent)
        {
            int startX = gameScreen.position.X - gameScreen.width / 2;
            int startY = gameScreen.position.Y - gameScreen.height / 2;

            // Определяем конечные координаты для отрисовки
            int endX = startX + gameScreen.width;
            int endY = startY + gameScreen.height;

            // Рендерим только видимую часть игровой матрицы
            int screenX = -1;
            int screenY = -1;
            for (int y = startY; y < endY; y++)
            {
                screenY++;
                for (int x = startX; x < endX; x++)
                {
                    screenX++;
                    if (x == gameScreen.position.X - 1 && y == gameScreen.position.Y)
                    {
                        outputContentBuilder.Append(">");
                        continue;
                    }
                    if (x == gameScreen.position.X + 1 && y == gameScreen.position.Y)
                    {
                        outputContentBuilder.Append("<");
                        continue;
                    }
                    if (x < 0 || y < 0 || x >= gameMatrix.width || y >= gameMatrix.height)
                    {
                        if ((screenX + screenY) % 2 == 0) outputContentBuilder.Append("d");
                        else outputContentBuilder.Append("s");
                        continue;
                    }
                    outputContentBuilder.Append(gameMatrix.getCharOfTile(x, y).ToString());
                }
                outputContentBuilder.Append("\n");
            }
            outputContentBuilder.Append("\n\n\n");
        }

        // Метод для отображения приглашения ввода
        private void ShowPrompt()
        {
            Write(PROMPT, inputConsoleColor, inputConsole);
        }

        // Обробка потоку вводу
        private void ProcessInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return;
            LogManager.addNote($"New input: {input}");

            Match setRelativeMatch = SET_RELATIVE_PATTERN.Match(input);
            if (setRelativeMatch.Success)
            {
                int X = int.Parse(setRelativeMatch.Groups[1].Value);
                int Y = int.Parse(setRelativeMatch.Groups[2].Value);
                string tileType = setRelativeMatch.Groups[3].Value.ToLower();
                string tileDirection = setRelativeMatch.Groups[4].Value.ToLower();
                try
                {
                    gameMatrix.setTileAtPoint(string2Type[tileType], gameScreen.position.X + X, gameScreen.position.Y + Y, string2Directions[tileDirection]);
                } 
                catch
                {

                }
                return;
            }

            Match setMatch = SET_PATTERN.Match(input);
            if (setMatch.Success)
            {
                string tileType = setMatch.Groups[1].Value.ToLower();
                string tileDirection = setMatch.Groups[2].Value.ToLower();
                try
                {
                    gameMatrix.setTileAtPoint(string2Type[tileType], gameScreen.position.X, gameScreen.position.Y, string2Directions[tileDirection]);
                }
                catch
                {

                }
                return;
            }

            Match setAreaMatch = SET_AREA_PATTERN.Match(input);
            if (setAreaMatch.Success)
            {
                LogManager.addNote("setAreaMatch.Success");
                int startX = int.Parse(setAreaMatch.Groups[1].Value);
                int startY = int.Parse(setAreaMatch.Groups[2].Value);
                int endX = int.Parse(setAreaMatch.Groups[3].Value);
                int endY = int.Parse(setAreaMatch.Groups[4].Value);

                string tileType = setAreaMatch.Groups[5].Value.ToLower();
                string tileDirection = setAreaMatch.Groups[6].Value.ToLower();
                LogManager.addNote($"{startX} {startY} {endX} {endY} {tileType} {tileDirection}");
                for (int y = Math.Min(startY, endY); y <= Math.Max(startY, endY); y++)
                {
                    for (int x = Math.Min(startX, endX); x <= Math.Max(startX, endX); x++)
                    {
                        try
                        {
                            gameMatrix.setTileAtPoint(string2Type[tileType], gameScreen.position.X + x, gameScreen.position.Y + y, string2Directions[tileDirection]);
                        }
                        catch
                        {
                            LogManager.addNote($"Wtf with setArea? tileDirection = {tileDirection} tileType = {tileType}");
                        }
                    }
                }
                return;
            }

            Match setGameVariable = SET_GAME_VARIABLE_PATTERN.Match(input);
            if (setGameVariable.Success)
            {
                string variable = setGameVariable.Groups[1].Value.ToLower();
                int value = int.Parse(setGameVariable.Groups[2].Value);

                LogManager.addNote($"setGameVariable.Success");
                try
                {
                    LogManager.addNote($"Switch(variable)");
                    switch (variable)
                    {
                        case "logictick":
                            timePerLogicTick = value;
                            break;
                        case "tick":
                            timePerTick = value;
                            break;
                    }
                } 
                catch 
                {

                }
                
                return;
            }

            if (SET_EMPTY_PATTERN.IsMatch(input))
            {
                gameMatrix.setTileAtPoint(TileType.Empty, gameScreen.position.X, gameScreen.position.Y);
                return;
            }
            if (CLEAR_PATTERN.IsMatch(input))
            {
                gameMatrix.clear();
                return;
            }

            Match saveMatch = SAVE_PATTERN.Match(input);
            if (saveMatch.Success)
            {
                string variable = saveMatch.Groups[1].Value;
                SaveLoadManager.SaveMap(variable, gameMatrix);
            }

            Match timeMatch = TIME_PATTERN.Match(input);
            if (timeMatch.Success)
            {
                string variable = timeMatch.Groups[1].Value.ToLower();
                switch (variable)
                {
                    case "stop":
                    case "pause":
                        isPaused = true; break;
                    case "unpause":
                    case "resume":
                        isPaused = false; break;
                }
            }

            Match loadMatch = LOAD_PATTERN.Match(input);
            if (loadMatch.Success)
            {
                string variable = loadMatch.Groups[1].Value;
                SaveLoadManager.LoadMap(variable, ref gameMatrix);
            }
        }

        private void WriteLine(string text, RichTextBox console)
        {
            Write($"{text}\n", console);
        }

        private void WriteLine(string text, Color color, RichTextBox console)
        {
            Write($"{text}\n", color, console);
        }

        private void Write(char character, Color color, RichTextBox console)
        {
            Write(character, 1, color, console);
        }

        private void Write(char character, int qty, RichTextBox console) // qty - quantity - кількість
        {
            Write(character, qty, Color.White, console);
        }

        private void Write(char character, int qty, Color color, RichTextBox console) // qty - quantity - кількість
        {
            Write(new string(character, qty), color, outputConsole);
        }

        private void Write(string text, RichTextBox console)
        {
            Write(text, Color.White, console);
        }

        private void Write(string text, Color color, RichTextBox console)
        {
            console.SelectionColor = color;
            console.AppendText(text);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //WriteLine(this.Size.Width.ToString(), Color.Red, outputConsole); - Debug
            outputConsole.Size = new Size(this.Size.Width - 3 * borderSize[0], this.Size.Height - outputConsoleHeight - 2 * borderSize[1]);
            inputConsole.Size = new Size(this.Size.Width - 3 * borderSize[0], outputConsoleHeight - 3 * borderSize[1]);
            inputConsole.Top = this.Size.Height - outputConsoleHeight;

            gameScreen.size = new Point(Convert.ToInt32(outputConsole.Width / charSize[0]), Convert.ToInt32(outputConsole.Height / charSize[1]));
        }
    }
}

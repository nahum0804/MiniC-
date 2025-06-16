using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Antlr4.Runtime;
using MiniCSharp.checker;
using MiniCSharp.codeGen;
using MiniCSharp.domain.errors;

namespace MiniCSharpIDE
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    /// <summary>TextReader que recibe líneas desde la GUI (bloquea en ReadLine).</summary>
    public class GuiTextReader : TextReader
    {
        private readonly BlockingCollection<string> _queue = new();

        /// <summary>Encola una línea escrita por el usuario (Enter).</summary>
        public void Push(string line) => _queue.Add(line);

        public override string? ReadLine() => _queue.Take();
    }
    
    public class GuiTextWriter : TextWriter
    {
        private readonly RichTextBox _box;
        private readonly Color _color;
        private readonly Encoding _enc = new UTF8Encoding();

        public GuiTextWriter(RichTextBox box, Color color)
        {
            _box  = box;
            _color = color;
        }

        public override Encoding Encoding => _enc;

        public override void Write(char value) => Write(value.ToString());

        public override void Write(string? value)
        {
            if (value is null) return;

            _box.Invoke(() =>
            {
                _box.SelectionColor = _color;
                _box.AppendText(value);
                _box.ScrollToCaret();
            });
        }

        public override void WriteLine(string? value) => Write(value + Environment.NewLine);
    }


    public sealed class MainForm : Form
    {
        private readonly TextBox _txtSource;  
        private readonly TextBox _txtStdin;    
        private readonly Button _btnCompileRun;
        private readonly RichTextBox _rtbOutput;

        private readonly GuiTextReader _stdinReader = new();               
        private readonly GuiTextWriter _guiStdOut;                         
        private readonly GuiTextWriter _guiStdErr;                         


        public MainForm()
        {
            Text = "MiniC# IDE";
            Width = 1000;
            Height = 700;
            BackColor = Color.Black;
            ForeColor = Color.White;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.White,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));   // Código
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));  // StdIn
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // Botón
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));   // Salida

            _txtSource = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            layout.Controls.Add(_txtSource, 0, 0);

            _txtStdin = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                PlaceholderText = "Entrada para read()  ―  escribe algo y presiona Enter"
            };
            _txtStdin.KeyDown += TxtStdin_KeyDown;
            layout.Controls.Add(_txtStdin, 0, 1);

            _btnCompileRun = new Button
            {
                Text = "Compilar / Correr",
                Dock = DockStyle.Fill,
                BackColor = Color.Cyan,
                ForeColor = Color.Black
            };
            _btnCompileRun.Click += BtnCompileRun_Click;
            layout.Controls.Add(_btnCompileRun, 0, 2);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F5) BtnCompileRun_Click(s, EventArgs.Empty); };

            _rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            _rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            layout.Controls.Add(_rtbOutput, 0, 3);

            _guiStdOut = new GuiTextWriter(_rtbOutput, Color.White); 
            _guiStdErr = new GuiTextWriter(_rtbOutput, Color.Red);  

            layout.Controls.Add(_rtbOutput, 0, 3);

            Controls.Add(layout);
            _txtSource.Text = GetSampleCode();
        }

        //public sealed override Color BackColor
        //{
        //    get => base.BackColor;
        //    set => base.BackColor = value;
        //}

        //[AllowNull] public sealed override string Text
        //{
        //   get => base.Text;
        //    set => base.Text = value;
        //}

        private void TxtStdin_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            _stdinReader.Push(_txtStdin.Text);

            _rtbOutput.Invoke(new Action(() =>
            {
                _rtbOutput.SelectionColor = Color.Lime;  
                _rtbOutput.AppendText(_txtStdin.Text + Environment.NewLine);
                _rtbOutput.ScrollToCaret();
            }));

            _txtStdin.Clear();
            e.SuppressKeyPress = true;
        }

        private async void BtnCompileRun_Click(object? sender, EventArgs e)
        {
            _btnCompileRun.Enabled = false;

            _rtbOutput.Clear();
            _rtbOutput.SelectionColor = Color.LimeGreen;
            _rtbOutput.AppendText("Compilando y ejecutando...\n\n");

            var originalOut = Console.Out;
            var originalErr = Console.Error;
            var originalIn  = Console.In;

            Console.SetOut(_guiStdOut);     
            Console.SetError(_guiStdErr);   
            Console.SetIn(_stdinReader);    

            try
            {
                await Task.Run(() => CompileAndRun());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FATAL] {ex.Message}");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                Console.SetIn(originalIn);

                _rtbOutput.Invoke(() =>
                {
                    _rtbOutput.SelectionColor = Color.LimeGreen;
                    _rtbOutput.AppendText("\n============ Fin de la ejecución ============\n");
                    _rtbOutput.SelectionColor = Color.Cyan;
                    _rtbOutput.AppendText(@"

 __  __     
|  \/  (_)     (_)
| \  / |_| |__ | |
| |\/| | |  _ \  |
| |  | | | | | | |
|_|  |_|_|_| |_|_|
            __     __
  ____   _ |  |__ |  |_
 / ___| |_     __      _|
| |      _|   |__ |   |_
| |___  | _    __      _|
 \____|    |__|   |__ |
" + Environment.NewLine);
                    _rtbOutput.SelectionColor = Color.White;
                });

                _btnCompileRun.Enabled = true;
            }
        }


        private void CompileAndRun()
        {
            var inputText  = _txtSource.Invoke<string>(() => _txtSource.Text);

            var inputStream = new AntlrInputStream(inputText);
            var lexer  = new MiniCSLexer(inputStream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new LexerErrorListener());

            var tokens = new CommonTokenStream(lexer);
            var parser = new MiniCSParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ParserErrorListener());
            var tree = parser.program();

            var symVisitor = new SymbolTableVisitor();
            symVisitor.Visit(tree);

            var checker = new MiniCSChecker { Table = symVisitor.Table };
            checker.Visit(tree);

            if (checker.HasErrors)
            {
                foreach (var err in checker.Errors)
                    Console.Error.WriteLine(err);
                return;
            }

            // Generación de código IL y ejecución
            var asmName = new AssemblyName("MiniCSharpProgram");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);

            var codeGen = new CodeGenVisitor(moduleBuilder, symVisitor.Table, checker.ExprTypes);
            codeGen.Generate(tree);

            var programType = asmBuilder.GetType("P");
            var mainMethod  = programType?.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
            mainMethod?.Invoke(null, null);
        }

        private static string GetSampleCode()
        {
            const string filePath = "Test.txt";

            return File.Exists(filePath)
                ? File.ReadAllText(filePath)
                : @"class P
{
    int sum(int a, int b) { return a + b; }

    void Main()
    {
        int n;
        write(""Ingresa un número: "");
        read(n);
        write(""El doble es: "");
        write( sum(n, n) );
    }
}";
        }
    }
}

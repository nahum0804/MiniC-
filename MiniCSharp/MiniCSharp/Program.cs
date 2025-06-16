using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
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

    public class GuiTextReader : TextReader
    {
        private readonly BlockingCollection<string> _queue = new();
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
            if (value == null) return;
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
        private readonly Button _btnCompileRun;
        private readonly RichTextBox _rtbOutput;

        private readonly GuiTextReader _stdinReader = new();
        private readonly GuiTextWriter _guiStdOut;
        private readonly GuiTextWriter _guiStdErr;
        private int _consoleInputStart;

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
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

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

            _btnCompileRun = new Button
            {
                Text = "Compilar / Correr (F5)",
                Dock = DockStyle.Fill,
                BackColor = Color.Fuchsia,
                ForeColor = Color.White
            };
            _btnCompileRun.Click += BtnCompileRun_Click;
            layout.Controls.Add(_btnCompileRun, 0, 1);

            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F5) BtnCompileRun_Click(s, EventArgs.Empty); };

            _rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White,
                ReadOnly = false
            };
            _rtbOutput.KeyPress += RtbOutput_KeyPress;
            _rtbOutput.KeyDown  += RtbOutput_KeyDown;
            layout.Controls.Add(_rtbOutput, 0, 2);

            Controls.Add(layout);
            _txtSource.Text = GetSampleCode();

            _guiStdOut = new GuiTextWriter(_rtbOutput, Color.White);
            _guiStdErr = new GuiTextWriter(_rtbOutput, Color.Red);
        }

        private void RtbOutput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (_rtbOutput.SelectionStart < _consoleInputStart)
                _rtbOutput.SelectionStart = _rtbOutput.TextLength;
        }

        private void RtbOutput_KeyDown(object sender, KeyEventArgs e)
        {
            if (_rtbOutput.ReadOnly) { e.SuppressKeyPress = true; return; }
            if (e.KeyCode == Keys.Back && _rtbOutput.SelectionStart <= _consoleInputStart)
            {
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Enter)
            {
                int lastNewline = _rtbOutput.Text.LastIndexOf('\n', _rtbOutput.TextLength - 1);
                int startIndex = Math.Max(lastNewline + 1, _consoleInputStart);
                string line = _rtbOutput.Text.Substring(startIndex, _rtbOutput.TextLength - startIndex)
                                            .TrimEnd('\r', '\n');
                _stdinReader.Push(line);
                _rtbOutput.AppendText(Environment.NewLine);
                _consoleInputStart = _rtbOutput.TextLength;
                e.SuppressKeyPress = true;
            }
        }

        private async void BtnCompileRun_Click(object sender, EventArgs e)
        {
            _btnCompileRun.Enabled = false;
            _rtbOutput.ReadOnly = false;
            _rtbOutput.Clear();
            _rtbOutput.SelectionColor = Color.LimeGreen;
            _rtbOutput.AppendText("Compilando y ejecutando...\n\n");
            _rtbOutput.SelectionColor = Color.White;
            _consoleInputStart = _rtbOutput.TextLength;
            _rtbOutput.Focus();

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
                Console.Error.WriteLine($"[FATAL] {ex.GetBaseException().Message}");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                Console.SetIn(originalIn);

                _rtbOutput.ReadOnly = true;
                _rtbOutput.SelectionColor = Color.LimeGreen;
                _rtbOutput.AppendText("\n============ Fin de la ejecución ============");
                _consoleInputStart = _rtbOutput.TextLength;
                _rtbOutput.SelectionColor = Color.Cyan;
                _rtbOutput.AppendText(@"
 __  __     
|  \/  (_)_     (_)
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
                _rtbOutput.AppendText("\n=============================================");
                _rtbOutput.SelectionColor = Color.White;
                _btnCompileRun.Enabled = true;
            }
        }

        private void CompileAndRun()
        {
            var inputText = _txtSource.Invoke<string>(() => _txtSource.Text);
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

            var asmName = new AssemblyName("MiniCSharpProgram");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);

            var codeGen = new CodeGenVisitor(moduleBuilder, symVisitor.Table, checker.ExprTypes);
            codeGen.Generate(tree);

            var programType = asmBuilder.GetType("P");
            var mainMethod  = programType?.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
            try
            {
                mainMethod?.Invoke(null, null);
            }
            catch (TargetInvocationException tie)
            {
                Console.Error.WriteLine($"[ERROR EJECUCIÓN] {tie.InnerException?.Message}");
            }
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
        write(sum(n, n));
    }
}";
        }
    }
}

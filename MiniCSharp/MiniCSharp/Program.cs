using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Drawing;
using System.Windows.Forms;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MiniCSharp.checker;
using MiniCSharp.codeGen;
using MiniCSharp.domain.errors;

namespace MiniCSharpIDE
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private TextBox txtInput;
        private Button btnCompileRun;
        private RichTextBox rtbOutput;

        public MainForm()
        {
            Text = "MiniC# IDE";
            Width = 1000;
            Height = 700;
            BackColor = Color.Black;
            ForeColor = Color.White;

            var layout = new TableLayoutPanel
                { Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.White };
            layout.RowCount = 3;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            txtInput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            layout.Controls.Add(txtInput, 0, 0);

            btnCompileRun = new Button
            {
                Text = "Compilar/Correr",
                Dock = DockStyle.Fill,
                Height = 30,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            btnCompileRun.Click += BtnCompileRun_Click;
            layout.Controls.Add(btnCompileRun, 0, 1);

            rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            layout.Controls.Add(rtbOutput, 0, 2);

            Controls.Add(layout);
            txtInput.Text = GetSampleCode();
        }

        private void BtnCompileRun_Click(object sender, EventArgs e)
        {
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            var sw = new StringWriter();
            Console.SetOut(sw);
            Console.SetError(sw);

            try
            {
                var inputText = txtInput.Text;
                var inputStream = new AntlrInputStream(inputText);
                var lexer = new MiniCSLexer(inputStream);
                lexer.RemoveErrorListeners();
                lexer.AddErrorListener(new LexerErrorListener());
                var tokens = new CommonTokenStream(lexer);
                var parser = new MiniCSParser(tokens);
                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ParserErrorListener());
                var tree = parser.program();

                var symVisitor = new SymbolTableVisitor();
                symVisitor.Visit(tree);
                var table = symVisitor.Table;
                var checker = new MiniCSChecker { Table = table };
                checker.Visit(tree);

                if (checker.HasErrors)
                {
                    foreach (var err in checker.Errors)
                        Console.Error.WriteLine(err);
                }
                else
                {
                    var asmName = new AssemblyName("MiniCSharpProgram");
                    var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                    var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);
                    var codeGen = new CodeGenVisitor(moduleBuilder, table, checker.ExprTypes);
                    codeGen.Generate(tree);

                    var programType = asmBuilder.GetType("P");
                    var mainMethod = programType?.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                    mainMethod?.Invoke(null, null);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error de ejecución: " + ex);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);

                rtbOutput.Clear();

                rtbOutput.SelectionColor = Color.LimeGreen;
                rtbOutput.AppendText("Compiled successfully - Happy coding :) \n \n" + Environment.NewLine);
                rtbOutput.SelectionColor = Color.White;
                rtbOutput.AppendText(sw.GetStringBuilder().ToString());
                rtbOutput.SelectionColor = Color.LimeGreen;
                rtbOutput.AppendText("\n\n============ Fin de la ejecucion ============");
                rtbOutput.SelectionColor = Color.LimeGreen;
                rtbOutput.AppendText("fin de la ejecucion" + Environment.NewLine);

                // Logo ASCII "Mini C"
                rtbOutput.SelectionColor = Color.Cyan;
                rtbOutput.AppendText(@"
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
                rtbOutput.SelectionColor = Color.White;
            }
        }

        private string GetSampleCode()
        {
            const string filePath = "Test.txt";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Archivo no encontrado: {Path.GetFullPath(filePath)}");
                return @"class P 
{
    int sum(int a, int b)
    {
        return a + b;
    }

    void Main()
    {
        int x; 
        x = sum(5, 40);
        write(x);
    }
}";
            }

            var inputText = File.ReadAllText(filePath);
            return inputText;
        }
    }
}
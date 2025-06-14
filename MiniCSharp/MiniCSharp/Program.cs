using System.Text;
using Antlr4.Runtime;
using generated.parser;
using MiniCSharp.checker;

namespace MiniCSharp;

// ============================================================================
// BACK‑END (parser + checker)
// ============================================================================
public static class MiniCSRunner
{
    public sealed record RunResult(string Console, bool IsSuccess);

    public static RunResult Analyze(string source)
    {
        var sb = new StringBuilder();

        // 1) Lexer -----------------------------------------------------------
        var lexer  = new MiniCSLexer(new AntlrInputStream(source));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new CollectingErrorListener<int>(sb, "LEXER"));

        // 2) Parser ----------------------------------------------------------
        var parser = new MiniCSParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new CollectingErrorListener<IToken>(sb, "PARSER"));

        // 3) AST + Checker ---------------------------------------------------
        var tree    = parser.program();
        var checker = new MiniCSChecker();
        checker.Visit(tree);

        // 4) Tabla de símbolos (preserva formato original) -------------------
        sb.AppendLine("\n--- TABLA DE SÍMBOLOS ---");
        var originalOut = Console.Out;
        using (var sw = new StringWriter())
        {
            Console.SetOut(sw);
            checker.Table.Print();
            Console.SetOut(originalOut);
            sb.Append(sw.ToString());
        }

        // 5) Estado de compilación ------------------------------------------
        bool hasParseErrors = parser.NumberOfSyntaxErrors > 0;
        bool success        = !(checker.HasErrors || hasParseErrors);

        if (!success)
        {
            sb.AppendLine("\n=== ERRORES DETECTADOS ===");
            if (checker.HasErrors)
            {
                foreach (var err in checker.Errors)
                    sb.AppendLine("  " + err);
            }
            else
            {
                sb.AppendLine("  Hay errores léxicos/sintácticos; revisa los mensajes anteriores.");
            }
        }
        else
        {
            sb.AppendLine("\nCompiled Successfully - Happy Coding :)\n");
        }

        return new RunResult(sb.ToString(), success);
    }
}

// ============================================================================
// Listener genérico para ANTLR 4.13.x
// ============================================================================
public sealed class CollectingErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
{
    private readonly StringBuilder _sb;
    private readonly string        _phase;

    public CollectingErrorListener(StringBuilder sb, string phase)
    {
        _sb    = sb;
        _phase = phase;
    }

    public void SyntaxError(TextWriter output,
                            IRecognizer recognizer,
                            TSymbol     offendingSymbol,
                            int         line,
                            int         charPositionInLine,
                            string      msg,
                            RecognitionException e)
    {
        _sb.AppendLine($"[{_phase}] Línea {line}:{charPositionInLine} — {msg}");
    }
}

// ============================================================================
// FRONT‑END (WinForms) – editor + consola, tema oscuro
// ============================================================================
public class MiniCSIDE : Form
{
    // ----------------------------- UI CONTROLS -----------------------------
    private readonly RichTextBox _editor = new()
    {
        Font       = new Font("Consolas", 10),
        AcceptsTab = true,
        WordWrap   = false,
        Dock       = DockStyle.Fill,
        BackColor  = Color.Black,
        ForeColor  = Color.White,
        Text       = "// Bienvenido a este nuevo IDE para el super lenguaje MiniC#"
    };

    private readonly Button _runButton = new()
    {
        Text      = "▶️ Compilar / Correr",
        AutoSize  = true,
        Dock      = DockStyle.Bottom,
        BackColor = Color.FromArgb(0, 255, 0),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Cursor    = Cursors.Hand,
        Font      = new Font("Segoe UI", 10, FontStyle.Bold)
    };

    private readonly RichTextBox _console = new()
    {
        Dock       = DockStyle.Fill,
        Multiline  = true,
        ReadOnly   = true,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        Font       = new Font("Consolas", 10),
        BackColor  = Color.Black,
        ForeColor  = Color.White
    };

    // ----------------------------- CONSTRUCTOR -----------------------------
    public MiniCSIDE()
    {
        // Ventana principal -------------------------------------------------
        Text       = "MiniC# IDE";
        Width      = 1000;
        Height     = 750;
        BackColor  = Color.Black;
        ForeColor  = Color.White;
        KeyPreview = true;

        // Estilo botón dinámico
        _runButton.FlatAppearance.BorderSize        = 0;
        _runButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 65, 65);
        _runButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 80);

                // Layout ------------------------------------------------------------
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, BackColor = Color.Black };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));      // editor-zone
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // button row
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));      // console-zone

        layout.Controls.Add(_editor   , 0, 0);
        layout.Controls.Add(_runButton, 0, 1);
        layout.Controls.Add(_console  , 0, 2);
        Controls.Add(layout);

        // Eventos -----------------------------------------------------------
        _runButton.Click += (_, __) => Run();
        KeyDown           += (_, e) => { if (e.KeyCode == Keys.F5) { Run(); e.Handled = true; } };
    }

    // ----------------------------- MÉTODO RUN ------------------------------
    private void Run()
    {
        var result = MiniCSRunner.Analyze(_editor.Text);

        _console.Clear();
        _console.ForeColor = Color.White;
        _console.Text      = result.Console;

        string marker = result.IsSuccess ? "Compiled Successfully" : "=== ERRORES DETECTADOS ===";
        int idx = _console.Text.IndexOf(marker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            _console.Select(idx, marker.Length);
            _console.SelectionColor = result.IsSuccess ? Color.LimeGreen : Color.Red;
            _console.SelectionLength = 0; // remove selection
        }
    }

    // ----------------------------- MAIN ENTRY ------------------------------
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MiniCSIDE());
    }
}

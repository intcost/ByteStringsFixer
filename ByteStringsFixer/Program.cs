using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

class ByteStringsFixer
{
    [STAThread]
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        PrintBanner();

        string path = null;

        if (args.Length > 0)
            path = args[0];
        else
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[~] Путь не передан. Открываю окно выбора файла...");
            Console.ResetColor();

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*";
            ofd.Title = "Выберите файл";
            if (ofd.ShowDialog() == DialogResult.OK)
                path = ofd.FileName;
            else
            {
                PrintError("Файл не выбран.");
                return;
            }
        }

        if (!File.Exists(path))
        {
            PrintError("Файл не найден: " + path);
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[*] Чтение: " + path);
        Console.ResetColor();

        string content = File.ReadAllText(path, Encoding.UTF8);

        string pattern = "\"((?:[^\"\\\\]|\\\\.)*?)\"";

        string fixedContent = Regex.Replace(content, pattern, match =>
        {
            string inner = match.Groups[1].Value;

            if (!inner.Contains(@"\x"))
                return match.Value;

            try
            {
                string decodedEscapes = DecodeEscapedString(inner);
                byte[] bytes = Encoding.GetEncoding("latin1").GetBytes(decodedEscapes);
                string finalStr = Encoding.GetEncoding(1251).GetString(bytes);
                finalStr = finalStr.Replace("\"", "\\\"");
                return $"\"{finalStr}\"";
            }
            catch
            {
                return match.Value;
            }
        });

        string watermark = GetWatermarkComment(path);

        if (!fixedContent.Contains("Fixed by ByteStringsFixer"))
            fixedContent = watermark + Environment.NewLine + fixedContent;

        string outputPath = Path.Combine(
            Path.GetDirectoryName(path),
            Path.GetFileNameWithoutExtension(path) + "_fixed" + Path.GetExtension(path)
        );

        File.WriteAllText(outputPath, fixedContent, Encoding.UTF8);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[✓] Готово! Сохранено в:");
        Console.WriteLine("    " + outputPath);
        Console.ResetColor();
    }

    static string GetWatermarkComment(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        string[] lines = new[]
        {
            "Fixed by ByteStringsFixer",
            "Author intcost",
            "github.com/intcost/ByteStringsFixer"
        };

        string commentStart;
        string commentLine;

        switch (ext)
        {
            case ".lua":
                commentLine = "-- ";
                break;
            case ".py":
            case ".sh":
            case ".bash":
            case ".zsh":
                commentLine = "# ";
                break;
            case ".c":
            case ".cpp":
            case ".cs":
            case ".java":
            case ".js":
            case ".ts":
            case ".jsx":
            case ".tsx":
            case ".json":
                commentLine = "// ";
                break;
            case ".ini":
            case ".cfg":
            case ".conf":
                commentLine = "; ";
                break;
            default:
                commentLine = "-- ";
                break;
        }

        StringBuilder sb = new StringBuilder();
        foreach (var line in lines)
            sb.AppendLine(commentLine + line);

        return sb.ToString();
    }

    static string DecodeEscapedString(string escaped)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < escaped.Length; i++)
        {
            if (escaped[i] == '\\' && i + 1 < escaped.Length)
            {
                char next = escaped[i + 1];
                if (next == 'x' && i + 3 < escaped.Length)
                {
                    string hex = escaped.Substring(i + 2, 2);
                    sb.Append((char)Convert.ToByte(hex, 16));
                    i += 3;
                }
                else if (next == 'n')
                {
                    sb.Append('\n');
                    i++;
                }
                else if (next == 'r')
                {
                    sb.Append('\r');
                    i++;
                }
                else if (next == 't')
                {
                    sb.Append('\t');
                    i++;
                }
                else if (next == '\\')
                {
                    sb.Append('\\');
                    i++;
                }
                else if (next == '"')
                {
                    sb.Append('\"');
                    i++;
                }
                else
                {
                    sb.Append('\\');
                }
            }
            else
            {
                sb.Append(escaped[i]);
            }
        }
        return sb.ToString();
    }

    static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Ошибка] " + message);
        Console.ResetColor();
    }

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"
╔════════════════════════════════════════════╗
║     ByteStringsFixer by intcost            ║
║  🔗 github.com/intcost/ByteStringsFixer    ║
╚════════════════════════════════════════════╝
");
        Console.ResetColor();
    }
}

using Markdig;
using Markdig.SyntaxHighlighting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Media = System.Windows.Media;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Notes {
  public static class NoteRenderer {

    private static readonly Regex dateTimePattern = new Regex(@"(%((date)|(time)|(datetime))(u)?)((:)(.*?))?(%)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex userPattern = new Regex(@"%user%", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex domainPattern = new Regex(@"%domain%", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex hostPattern = new Regex(@"%host%", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ipPattern = new Regex(@"%ip%", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex envPattern = new Regex(@"(%env:)(.*?)(%)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex shellPattern = new Regex(@"(%shell:)(.*?)(%)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static MarkdownPipeline pipeline = new MarkdownPipelineBuilder().
                                                        UseAdvancedExtensions().
                                                        UseSyntaxHighlighting().
                                                        UseTaskLists().
                                                        UseTableOfContent().
                                                        UseEmojiAndSmiley().
                                                        Build();

    public static string RenderNote(NoteConfig noteConfig, Media.Color BgColor) {
      switch (noteConfig.DisplayType) {
        case DisplayTypes.Org:
          return RenderOrgMode(noteConfig.Content, BgColor);
        case DisplayTypes.MarkDown:
          return RenderMarkDown(noteConfig.Content, BgColor);
        case DisplayTypes.Custom:
          return RenderCustom(noteConfig.Content, BgColor);
        default:
          return RenderPlainText(noteConfig.Content, BgColor);
      }
    }

    public static string GlobalReplacements(string s) {
      foreach (ReplacePattern rp in Config.GlobalStylePatterns) {
        s = rp.Replace(s);
      }
      return s;
    }

    public static string WrapIntoHtml(string html, Media.Color BgColor) {
      Media.Color FgColor = (Media.Color)(Application.Current.FindResource("FgColor"));
      Media.Color LkColor = (Media.Color)(Application.Current.FindResource("LkColor"));
      List<string> lines = new List<string>() {
        "<!DOCTYPE html>",
        "<html>",
        "  <head>",
        "    <meta charset=\"UTF-8\">",
        "    <title>Note</title>",
        "    <style>",
        Properties.Resources.Style.
          Replace("PaleGoldenrod", $"rgb({BgColor.R},{BgColor.G},{BgColor.B})").
          Replace("MediumSlateBlue", $"rgb({FgColor.R},{FgColor.G},{FgColor.B})").
          Replace("CornflowerBlue", $"rgb({LkColor.R},{LkColor.G},{LkColor.B})"),
        "    </style>",
        "  </head>",
        "  <body>",
        GlobalReplacements(html),
        "    <script type=\"text/javascript\">",
        "    <!--",
        Properties.Resources.Script,
        "    //-->",
        "    </script>",
        "  </body>",
        "</html>"
      };
      return string.Join("\n", lines.ToArray());
    }

    public static string RenderPlainText(string content, Media.Color BgColor) {
      return WrapIntoHtml(
        ReplaceData(content).
          Replace("&", "&#38;").
          Replace("\t", "&#9;").
          Replace("<", "&#38;").
          Replace(">", "&#38;").
          Replace(" ", "&#160;").
          Replace("\r\n", "<br />").
          Replace("\r", "<br />").
          Replace("\n", "<br />").
          Replace("\"", "&#34;"),
        BgColor
      );
    }

    private static string ReplaceDateTime(Match m) {
      string type = m.Groups[2]?.Value;
      string utc = m.Groups[6]?.Value;
      string pattern = m.Groups[9]?.Value;
      if (string.IsNullOrEmpty(pattern)) {
        switch (type.ToLower()) {
          case "date":
            pattern = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;
            break;
          case "time":
            pattern = CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
            break;
          default:
            pattern = CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
            break;
        }
      }
      bool toUtc = utc.ToLower() == "u";
      System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
      doc.AppendChild(doc.CreateElement("t")).InnerXml = pattern;
      doc.DocumentElement.SetAttribute("p", doc.DocumentElement.InnerText);
      doc.DocumentElement.InnerXml = "";
      string xmlStr = doc.DocumentElement.OuterXml;
      xmlStr = xmlStr.Replace("<t p=\"", "").Replace("\"></t>", "");
      return $"<span id=\"date-time-field-{Guid.NewGuid()}\" class=\"date-time-field\" data-utc=\"{(toUtc ? 1 : 0)}\" data-format=\"{xmlStr}\">???</span>";
    }

    private static string GetIp() {
      string hostName = Dns.GetHostName();
      IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
      IPAddress[] ips = ipAddresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)).ToArray();
      string[] localIPs = ips.Select(ip => ip.ToString()).ToArray();
      if (localIPs.Length > 0) {
        return string.Join("; ", localIPs);
      }
      else {
        return "NONE";
      }
    }

    private static string GetEnv(Match m) {
      string envName = m.Groups[2]?.Value;
      string envVal = Environment.GetEnvironmentVariable(envName);
      return envVal;
    }

    private static string GetShell(Match m) {
      string shellCommand = m.Groups[2]?.Value;
      string[] parts = shellCommand.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
      ProcessStartInfo psi = new ProcessStartInfo();
      psi.FileName = parts[0];
      psi.Arguments = string.Join(" ", parts.Skip(1).ToArray());
      psi.UseShellExecute = false;
      psi.WindowStyle = ProcessWindowStyle.Hidden;
      psi.RedirectStandardOutput = true;
      psi.RedirectStandardError = true;
      psi.RedirectStandardInput = true;
      try {
        using (Process process = Process.Start(psi)) {
          if (process != null) {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return $"{output}{error}";
          }
          else {
            return "ERROR: Failed to start process";
          }
        }
      }
      catch (Exception ex) {
        return $"ERROR: {ex.Message}";
      }
    }

    private static string ReplaceData(string content) {
      content = userPattern.Replace(content, Environment.UserName);
      content = domainPattern.Replace(content, Environment.UserDomainName);
      content = hostPattern.Replace(content, Environment.MachineName);
      content = ipPattern.Replace(content, GetIp());
      content = envPattern.Replace(content, GetEnv);
      content = shellPattern.Replace(content, GetShell);
      return content;
    }

    public static string RenderMarkDown(string content, Media.Color BgColor) {
      string html = Markdown.ToHtml(ReplaceData(content), pipeline);
      html = dateTimePattern.Replace(html, ReplaceDateTime);
      return WrapIntoHtml(html, BgColor);
    }

    public static string RenderOrgMode(string content, Media.Color BgColor) {
      MessageBox.Show("Org Mode is not jet implemented!", "NOPE", MessageBoxButton.OK, MessageBoxImage.Stop);
      return ReplaceData(content);
    }

    public static string RenderCustom(string content, Media.Color BgColor) {
      content = ReplaceData(content);
      foreach(ReplacePattern rp in Config.CustomStylePatterns) {
        content = rp.Replace(content);
      }
      return WrapIntoHtml(content, BgColor);
    }
  }
}

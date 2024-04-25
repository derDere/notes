using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Notes {
  public class ReplacePattern {

    private Regex re = null;

    public string Pattern { get; set; } = @"[^\w\W]";
    public string Replacement { get; set; } = "";
    public RegexOptions RegexOptions { get; set; } = RegexOptions.IgnoreCase | RegexOptions.Singleline;

    public ReplacePattern() { }

    public ReplacePattern(string pattern, string replacement, RegexOptions option = RegexOptions.IgnoreCase | RegexOptions.Singleline) {
      Pattern = pattern;
      Replacement = replacement;
      RegexOptions = option;
    }

    public string Replace(string s) {
      if (re == null) {
        re = new Regex(Pattern, RegexOptions | RegexOptions.Compiled);
      }
      return re.Replace(s, Replacement);
    }
  }
}

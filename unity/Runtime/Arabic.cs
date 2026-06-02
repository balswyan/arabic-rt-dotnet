// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
using System;
using System.Collections.Generic;
using System.Text;

namespace ArabicRt
{
    /// <summary>Behaviour switches for <see cref="Arabic.Fix"/>. Defaults match the
    /// arabic_reshaper + python-bidi reference output; the rest are opt-in tricks for
    /// real-time clients (game chat) that render naively or read word-by-word.</summary>
    public sealed class Options
    {
        public bool CombineAllah = false;       // collapse الله -> ﷲ
        public bool KeepLtrRuns = true;         // keep Latin/number/URL runs in reading order
        public bool ReverseWordOrder = true;    // true: full RTL line; false: per-word, keep typed order
        public string WordJoiner = " ";         // separator between words (e.g. "\u00A0")
        public bool PreventWordSplit = false;   // replace spaces with WordJoiner for naive readers
        public int MaxLineChars = 0;            // >0: wrap into N-char lines (first words on top)

        /// <summary>Preset tuned for game chat (word-by-word readers, e.g. R.E.P.O.).</summary>
        public static Options Game => new Options
        { CombineAllah = true, WordJoiner = "\u00A0", PreventWordSplit = true };
    }

    /// <summary>Arabic shaping, simplified BiDi, and un-baking. Pure, dependency-free.
    /// Output is byte-identical to the Python `arabic-rt` package.</summary>
    public static class Arabic
    {
        private struct Glyph
        {
            public char Join; public int Iso, Ini, Med, Fin;
            public Glyph(char j, int iso, int ini, int med, int fin){ Join=j; Iso=iso; Ini=ini; Med=med; Fin=fin; }
        }

        private static readonly Dictionary<int, Glyph> Forms = new Dictionary<int, Glyph>
        {
            {0x0621,new Glyph('U',0xFE80,0,0,0)},{0x0622,new Glyph('R',0xFE81,0,0,0xFE82)},
            {0x0623,new Glyph('R',0xFE83,0,0,0xFE84)},{0x0624,new Glyph('R',0xFE85,0,0,0xFE86)},
            {0x0625,new Glyph('R',0xFE87,0,0,0xFE88)},{0x0626,new Glyph('D',0xFE89,0xFE8B,0xFE8C,0xFE8A)},
            {0x0627,new Glyph('R',0xFE8D,0,0,0xFE8E)},{0x0628,new Glyph('D',0xFE8F,0xFE91,0xFE92,0xFE90)},
            {0x0629,new Glyph('R',0xFE93,0,0,0xFE94)},{0x062A,new Glyph('D',0xFE95,0xFE97,0xFE98,0xFE96)},
            {0x062B,new Glyph('D',0xFE99,0xFE9B,0xFE9C,0xFE9A)},{0x062C,new Glyph('D',0xFE9D,0xFE9F,0xFEA0,0xFE9E)},
            {0x062D,new Glyph('D',0xFEA1,0xFEA3,0xFEA4,0xFEA2)},{0x062E,new Glyph('D',0xFEA5,0xFEA7,0xFEA8,0xFEA6)},
            {0x062F,new Glyph('R',0xFEA9,0,0,0xFEAA)},{0x0630,new Glyph('R',0xFEAB,0,0,0xFEAC)},
            {0x0631,new Glyph('R',0xFEAD,0,0,0xFEAE)},{0x0632,new Glyph('R',0xFEAF,0,0,0xFEB0)},
            {0x0633,new Glyph('D',0xFEB1,0xFEB3,0xFEB4,0xFEB2)},{0x0634,new Glyph('D',0xFEB5,0xFEB7,0xFEB8,0xFEB6)},
            {0x0635,new Glyph('D',0xFEB9,0xFEBB,0xFEBC,0xFEBA)},{0x0636,new Glyph('D',0xFEBD,0xFEBF,0xFEC0,0xFEBE)},
            {0x0637,new Glyph('D',0xFEC1,0xFEC3,0xFEC4,0xFEC2)},{0x0638,new Glyph('D',0xFEC5,0xFEC7,0xFEC8,0xFEC6)},
            {0x0639,new Glyph('D',0xFEC9,0xFECB,0xFECC,0xFECA)},{0x063A,new Glyph('D',0xFECD,0xFECF,0xFED0,0xFECE)},
            {0x0640,new Glyph('C',0x0640,0x0640,0x0640,0x0640)},{0x0641,new Glyph('D',0xFED1,0xFED3,0xFED4,0xFED2)},
            {0x0642,new Glyph('D',0xFED5,0xFED7,0xFED8,0xFED6)},{0x0643,new Glyph('D',0xFED9,0xFEDB,0xFEDC,0xFEDA)},
            {0x0644,new Glyph('D',0xFEDD,0xFEDF,0xFEE0,0xFEDE)},{0x0645,new Glyph('D',0xFEE1,0xFEE3,0xFEE4,0xFEE2)},
            {0x0646,new Glyph('D',0xFEE5,0xFEE7,0xFEE8,0xFEE6)},{0x0647,new Glyph('D',0xFEE9,0xFEEB,0xFEEC,0xFEEA)},
            {0x0648,new Glyph('R',0xFEED,0,0,0xFEEE)},{0x0649,new Glyph('R',0xFEEF,0,0,0xFEF0)},
            {0x064A,new Glyph('D',0xFEF1,0xFEF3,0xFEF4,0xFEF2)},
        };
        private static readonly Dictionary<int,int[]> LamAlef = new Dictionary<int,int[]>
        { {0x0622,new[]{0xFEF5,0xFEF6}},{0x0623,new[]{0xFEF7,0xFEF8}},{0x0625,new[]{0xFEF9,0xFEFA}},{0x0627,new[]{0xFEFB,0xFEFC}} };

        private const string AllahStr = "\u0627\u0644\u0644\u0647";
        private static readonly Dictionary<int,int> ToBase = BuildInverse();
        private static readonly Dictionary<int,string> LamAlefInverse = BuildLamAlefInverse();
        private static Dictionary<int,int> BuildInverse(){ var d=new Dictionary<int,int>(); foreach(var kv in Forms){ var g=kv.Value; foreach(var f in new[]{g.Iso,g.Ini,g.Med,g.Fin}) if(f!=0&&!d.ContainsKey(f)) d[f]=kv.Key; } return d; }
        private static Dictionary<int,string> BuildLamAlefInverse(){ var d=new Dictionary<int,string>(); foreach(var kv in LamAlef){ foreach(var lig in kv.Value) d[lig]="\u0644"+(char)kv.Key; } return d; }

        private static char JType(int cp)
        {
            Glyph g; if (Forms.TryGetValue(cp, out g)) return g.Join;
            if ((cp>=0x064B&&cp<=0x065F)||cp==0x0670||(cp>=0x06D6&&cp<=0x06DC)||
                (cp>=0x06DF&&cp<=0x06E4)||(cp>=0x06E7&&cp<=0x06E8)||(cp>=0x06EA&&cp<=0x06ED)) return 'T';
            return 'U';
        }
        private static bool IsArabicLetter(int cp){ return (cp>=0x0600&&cp<=0x06FF)||(cp>=0x0750&&cp<=0x077F); }

        public static bool ContainsArabic(string s){ if(string.IsNullOrEmpty(s))return false; foreach(char c in s) if(IsArabicLetter(c))return true; return false; }
        public static bool IsShaped(string s){ if(string.IsNullOrEmpty(s))return false; foreach(char c in s) if((c>=0xFB50&&c<=0xFDFF)||(c>=0xFE70&&c<=0xFEFF))return true; return false; }

        private static int PrevNT(int[] c,int i){ int j=i-1; while(j>=0&&JType(c[j])=='T')j--; return j; }
        private static int NextNT(int[] c,int n,int i){ int j=i+1; while(j<n&&JType(c[j])=='T')j++; return j; }

        private static string CollapseAllah(string text)
        {
            int idx=text.IndexOf(AllahStr,StringComparison.Ordinal); if(idx<0)return text;
            var sb=new StringBuilder(text.Length); int pos=0;
            while(idx>=0){ bool b=idx==0||!IsArabicLetter(text[idx-1]); int af=idx+AllahStr.Length;
                bool a=af>=text.Length||!IsArabicLetter(text[af]); sb.Append(text,pos,idx-pos);
                sb.Append(b&&a?"\uFDF2":AllahStr); pos=af; idx=text.IndexOf(AllahStr,pos,StringComparison.Ordinal); }
            sb.Append(text,pos,text.Length-pos); return sb.ToString();
        }

        public static string Shape(string text, bool combineAllah = false)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (combineAllah) text = CollapseAllah(text);
            int n=text.Length; var cps=new int[n]; for(int k=0;k<n;k++)cps[k]=text[k];
            var sb=new StringBuilder(n); int i=0;
            while(i<n){ int cp=cps[i]; char jt=JType(cp);
                if(cp==0x0644){ int ni=NextNT(cps,n,i);
                    if(ni<n&&LamAlef.ContainsKey(cps[ni])){ int pj=PrevNT(cps,i); char pjt=pj>=0?JType(cps[pj]):'U';
                        bool lp=(pjt=='D'||pjt=='C'); var lig=LamAlef[cps[ni]]; sb.Append((char)(lp?lig[1]:lig[0]));
                        for(int k=i+1;k<ni;k++)sb.Append((char)cps[k]); i=ni+1; continue; } }
                Glyph g; if(!Forms.TryGetValue(cp,out g)){ sb.Append((char)cp); i++; continue; }
                int p=PrevNT(cps,i),nx=NextNT(cps,n,i); char pt=p>=0?JType(cps[p]):'U',nt=nx<n?JType(cps[nx]):'U';
                bool jp=(jt=='D'||jt=='R')&&(pt=='D'||pt=='C'); bool jn=(jt=='D'||jt=='C')&&(nt=='D'||nt=='R'||nt=='C');
                int form = jp&&jn ? (g.Med!=0?g.Med:(g.Fin!=0?g.Fin:g.Iso)) : jp ? (g.Fin!=0?g.Fin:g.Iso) : jn ? (g.Ini!=0?g.Ini:g.Iso) : g.Iso;
                sb.Append((char)form); i++; }
            return sb.ToString();
        }

        private static bool IsLtr(int c){ return (c>=0x41&&c<=0x5A)||(c>=0x61&&c<=0x7A)||(c>=0xC0&&c<=0x24F)||c==0x200E; }
        private static bool IsDigit(int c){ return (c>=0x30&&c<=0x39)||(c>=0x0660&&c<=0x0669)||(c>=0x06F0&&c<=0x06F9); }
        private static bool IsGlue(int c){ switch((char)c){case '.':case '@':case ':':case '/':case '_':case '-':case '+':case '%':case '#':case '&':case '=':case '?':case ',':return true;default:return false;} }

        private static string BidiLine(string s, bool keepLtr)
        {
            int n=s.Length; var rev=new int[n]; for(int k=0;k<n;k++)rev[k]=s[n-1-k];
            var sb=new StringBuilder(n); int i=0;
            while(i<n){ int cp=rev[i]; bool lt=IsLtr(cp)||IsDigit(cp);
                if(keepLtr&&lt){ int j=i; while(j<n){ int c=rev[j];
                    if(IsLtr(c)||IsDigit(c))j++;
                    else if((c==0x20||IsGlue(c))&&j+1<n&&(IsLtr(rev[j+1])||IsDigit(rev[j+1])))j++; else break; }
                    for(int k=j-1;k>=i;k--)sb.Append((char)rev[k]); i=j; }
                else { sb.Append((char)cp); i++; } }
            return sb.ToString();
        }

        private static IEnumerable<string> WrapLogical(string line,int max)
        {
            if(max<=0||line.Length<=max){ yield return line; yield break; }
            var sb=new StringBuilder();
            foreach(var w in line.Split(' ')){ if(w.Length==0)continue;
                if(sb.Length==0)sb.Append(w);
                else if(sb.Length+1+w.Length<=max)sb.Append(' ').Append(w);
                else { yield return sb.ToString(); sb.Length=0; sb.Append(w); } }
            if(sb.Length>0) yield return sb.ToString();
        }

        private static string ProcessLine(string line, Options o)
        {
            line=line.Trim(' ','\t'); if(line.Length==0)return line;
            string result;
            if(o.ReverseWordOrder) result=BidiLine(Shape(line,o.CombineAllah),o.KeepLtrRuns).Trim(' ','\t');
            else { var parts=line.Split(' '); for(int i=0;i<parts.Length;i++) if(ContainsArabic(parts[i])) parts[i]=BidiLine(Shape(parts[i],o.CombineAllah),o.KeepLtrRuns); result=string.Join(" ",parts); }
            if(o.PreventWordSplit&&o.WordJoiner!=" ") result=result.Replace(" ",o.WordJoiner);
            return result;
        }

        public static string Fix(string text, Options opts = null)
        {
            if(string.IsNullOrEmpty(text))return text;
            var o=opts??new Options();
            if(!ContainsArabic(text))return text;
            if(IsShaped(text))return text;
            var lines=text.Replace("\r","").Split('\n'); var outl=new List<string>();
            foreach(var rl in lines) foreach(var ch in WrapLogical(rl,o.MaxLineChars)) outl.Add(ProcessLine(ch,o));
            return string.Join("\n",outl);
        }

        public static string Unfix(string text)
        {
            if(string.IsNullOrEmpty(text)||!IsShaped(text))return text;
            var lines=text.Replace("\r","").Split('\n'); var outl=new List<string>();
            foreach(var line in lines){ string logical=BidiLine(line,true); var sb=new StringBuilder(logical.Length);
                foreach(char ch in logical){ int cp=ch;
                    if(cp==0xFDF2){ sb.Append(AllahStr); continue; }
                    string la; if(LamAlefInverse.TryGetValue(cp,out la)){ sb.Append(la); continue; }
                    int b; if(ToBase.TryGetValue(cp,out b)){ sb.Append((char)b); continue; }
                    if(ch=='\u00A0'){ sb.Append(' '); continue; }
                    sb.Append(ch); }
                outl.Add(sb.ToString().Trim()); }
            return string.Join(" ",outl).Trim();
        }
    }
}

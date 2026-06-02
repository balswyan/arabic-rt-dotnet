using System; using System.IO; using System.Text; using ArabicRt;
class ParityTest {
    static void Main(){
        var w = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)){ AutoFlush=true };
        Console.SetOut(w);
        foreach(var raw in File.ReadAllLines("phrases.txt")){
            if(raw.Length==0) continue;
            var p=raw.Split(new[]{'\t'},2); string mode=p[0], text=p[1];
            var o = mode=="game" ? Options.Game : new Options();
            string s=Arabic.Shape(text,o.CombineAllah), f=Arabic.Fix(text,o), u=Arabic.Unfix(f);
            Console.WriteLine(string.Join("\t", new[]{s,f,u}));
        }
    }
}

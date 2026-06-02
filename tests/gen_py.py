import sys; sys.path.insert(0, "../../arabic-rt/src")
import arabic_rt as ar
for line in open("phrases.txt", encoding="utf-8"):
    line=line.rstrip("\n")
    if not line: continue
    mode,text=line.split("\t",1)
    o = ar.GAME if mode=="game" else ar.Options()
    s=ar.shape(text, combine_allah=o.combine_allah); f=ar.fix(text,o); u=ar.unfix(f)
    print("\t".join([s,f,u]))

# GameDesign/TechTrees.md -> Core/Progression/ResearchData.cs (üretilmiş dosya, elle düzenleme)
import re,sys
src=open('GameDesign/TechTrees.md',encoding='utf-8').read().split('\n')
treeNames=['EKONOMİ','SAVUNMA','ASKERÎ','MİMARLIK','KÖY GELİŞİMİ','BİLİM','ELEMENT']
rows=[]  # (tree, level, col, name)
tree=None; sec=None
for l in src:
    m=re.match(r'^# (\d)\. ',l)
    if m: tree=int(m.group(1))-1; sec=None; continue
    if tree is None: continue
    m=re.match(r'^## Seviye (\d+)',l)
    if m: sec=int(m.group(1))
    if not l.startswith('|') or l.startswith('|---'): continue
    cells=[c.strip() for c in l.strip().strip('|').split('|')]
    if tree<6 or tree==6:
        if cells[0].isdigit() and len(cells)==4:
            lv=int(cells[0])
            for i,c in enumerate(cells[1:]):
                if c and c!='—': rows.append((tree,lv,i+1,c))
        elif tree==6 and sec in (1,2,3,4) and len(cells)==2 and cells[0] not in ('Araştırma','---'):
            # temel element bilgisi: Ateş/Buz/Zehir sütun sırası = satır sırası
            idx=sum(1 for r in rows if r[0]==6 and r[1]==sec)+1
            rows.append((6,sec,idx,cells[0]))
cnt={}
for r in rows: cnt[r[0]]=cnt.get(r[0],0)+1
print(cnt,sum(cnt.values()),file=sys.stderr)
out=['// ÜRETİLMİŞ DOSYA: Tools/gen_research.py ile GameDesign/TechTrees.md\'den oluşturuldu. Elle düzenleme.',
'namespace GameCore.Progression','{','    internal static class ResearchData','    {','        // ağaç|seviye|sütun|ad  (ağaç: 0 Ekonomi,1 Savunma,2 Askerî,3 Mimarlık,4 Köy Gelişimi,5 Bilim,6 Element)',
'        public static readonly string[] Rows =','        {']
for t,lv,c,n in rows: out.append('            "%d|%d|%d|%s",'%(t,lv,c,n))
out+=['        };','    }','}']
open('Core/Progression/ResearchData.cs','w',encoding='utf-8').write('\n'.join(out)+'\n')

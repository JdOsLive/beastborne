"""Which classes in ROOT.razor.scss can match anything in ROOT's render subtree?
A stylesheet only reaches its own panel + descendants, so a class is live only if the
markup/code of the panel or a component mounted beneath it (transitively) mentions it.
usage: python tools/ui_subtreecss.py Code/UI/Panels/ExpeditionPanel.razor [--show-tree]
Prints the component tree (optional), counts, and a comma list of classes the sheet has
that nothing in reach can match. Conservative: counts every file that mounts the panel
(parents can pass content / class=) and all shared .cs; runtime prefixes and enum-derived
names (Earth -> .earth) count as used. Pair with a grep before deleting. Stylesheet scope
was verified live 2026-09-25 (see laws.md)."""
import re, os, glob, sys
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(ROOT)
start = sys.argv[1]

razor = {os.path.basename(f)[:-len('.razor')]: f.replace(os.sep, '/')
         for f in glob.glob('Code/**/*.razor', recursive=True)}
cs_by_class = {}
for f in glob.glob('Code/**/*.cs', recursive=True):
    txt = open(f, encoding='utf-8', errors='ignore').read()
    for m in re.finditer(r'\b(?:partial\s+)?class\s+(\w+)', txt):
        cs_by_class.setdefault(m.group(1), set()).add(f.replace(os.sep, '/'))

def text_of(name):
    parts = []
    if name in razor:
        parts.append(open(razor[name], encoding='utf-8', errors='ignore').read())
    for f in cs_by_class.get(name, ()):
        parts.append(open(f, encoding='utf-8', errors='ignore').read())
    return "\n".join(parts)

def children(txt):
    kids = set(re.findall(r'<([A-Z]\w+)[\s/>]', txt))              # <BattleView ... />
    kids |= set(re.findall(r'AddChild<(\w+)>', txt))                # AddChild<Foo>()
    kids |= set(re.findall(r'new\s+(\w+)\s*\(', txt)) & set(razor)  # new Foo(...) panels
    return {k for k in kids if k in razor or k in cs_by_class}

name = os.path.basename(start)[:-len('.razor')]
seen, stack, tree = set(), [name], {}
while stack:
    n = stack.pop()
    if n in seen: continue
    seen.add(n)
    kids = children(text_of(n)) - {n}
    tree[n] = sorted(kids)
    stack.extend(kids)
if '--show-tree' in sys.argv:
    for k in sorted(tree): print(f"{k}: {', '.join(tree[k])}")

corpus = "\n".join(text_of(n) for n in seen)
# A parent can put markup (ChildContent / RenderFragments) or a class="" on this
# component's root — that markup is styled by THIS sheet. Count every file that
# mounts the root component as reachable text too (conservative).
for other, f in razor.items():
    if other in seen: continue
    t = open(f, encoding='utf-8', errors='ignore').read()
    if re.search(r'<' + re.escape(name) + r'[\s/>]', t):
        corpus += "\n" + t
# Shared C# helpers (class-name builders, token tables) can feed any panel.
for f in glob.glob('Code/**/*.cs', recursive=True):
    corpus += "\n" + open(f, encoding='utf-8', errors='ignore').read()
tokens = set(re.findall(r'[A-Za-z_][\w-]*', corpus))
prefixes = set(re.findall(r'([A-Za-z_][\w-]*-)@', corpus))
prefixes |= set(re.findall(r'(?<![\w@-])([A-Za-z_][\w-]*)@\(', corpus))
prefixes |= set(re.findall(r'"[^"\n]*?([A-Za-z_][\w-]*)"\s*\+', corpus))
prefixes |= set(re.findall(r'\$"[^"\n]*?([A-Za-z_][\w-]*)\{', corpus))

prefixes |= set(re.findall(r'"([A-Za-z_][\w-]*-)"', corpus))   # any "foo-" literal (ternaries etc.)
scss = open(start + '.scss', encoding='utf-8').read()
scss_nc = re.sub(r'/\*.*?\*/', '', scss, flags=re.S)
scss_nc = re.sub(r'(?<!:)//[^\n]*', '', scss_nc)
scss_nc = re.sub(r'^\s*[\w-]+\s*:[^{]*;\s*$', '', scss_nc, flags=re.M)
scss_nc = re.sub(r'url\([^)]*\)', '', scss_nc)
classes = {c for c in re.findall(r'(?<!\d)\.(-?[A-Za-z_][\w-]*)', scss_nc)}
lower_tokens = {t.lower() for t in tokens}
dead = sorted(c for c in classes if c not in tokens and c.lower() not in lower_tokens and not any(c.startswith(p) for p in prefixes))
print(f"subtree: {len(seen)} components")
print(f"classes in sheet: {len(classes)}  unreachable: {len(dead)}")
print(",".join(dead))

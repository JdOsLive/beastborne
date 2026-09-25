#!/usr/bin/env python3
"""
Beastborne dead-CSS finder — lists SCSS classes that nothing in the code appears to use.

    python tools/ui_deadcss.py              # summary per stylesheet (most dead first)
    python tools/ui_deadcss.py --list       # also print the class names
    python tools/ui_deadcss.py FILE.scss    # just one stylesheet, with names

Stdlib only, no AI. A class counts as USED if its name appears anywhere in Code/**/*.razor
or *.cs anywhere (a coarse check — sheets are really SCOPED to their panel's subtree,
so this under-reports; see laws.md), or if it starts
with a dynamic prefix the code builds (e.g. `pop-@ver`, `"elem-" + x`, `$"rarity-{r}"`).

It's a heuristic: treat results as "probably dead — confirm with a grep before deleting".
False positives happen for classes built in unusual ways; false negatives happen when a
dead class name is also an ordinary word somewhere in the code.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CODE = ROOT / "Code"
UI = CODE / "UI"

_BLOCK = re.compile(r"/\*.*?\*/", re.S)
_LINE = re.compile(r"(?<!:)//[^\n]*")
# A class is `.name` not preceded by a digit (so `.a.b` chains count both
# classes, while 0.5s / 1.2em stay out; property lines are stripped anyway).
_CLASS = re.compile(r"(?<!\d)\.(-?[A-Za-z_][\w-]*)")
_PROP_LINE = re.compile(r"^\s*[\w-]+\s*:[^{]*;\s*$", re.M)  # declarations (skip: .5s, url(x.png))


def classes_in(scss: str) -> set[str]:
    text = _LINE.sub("", _BLOCK.sub("", scss))
    text = _PROP_LINE.sub("", text)
    text = re.sub(r"url\([^)]*\)", "", text)
    out = set()
    for m in _CLASS.finditer(text):
        name = m.group(1)
        if re.fullmatch(r"\d.*", name):
            continue
        out.add(name)
    return out


def _prefixes_in(text: str) -> set[str]:
    found = set(re.findall(r"([A-Za-z_][\w-]*-)@", text))            # class="foo-@x"
    found |= set(re.findall(r"(?<![\w@-])([A-Za-z_][\w-]*)@\(", text))  # class="p@(i)"
    found |= set(re.findall(r"\"[^\"\n]*?([A-Za-z_][\w-]*)\"\s*\+", text))  # "foo-" + x / "a sk-col-" + i
    found |= set(re.findall(r"\$\"[^\"]*?([A-Za-z_][\w-]*)\{", text))  # $"foo-{x}" / $"p{i}"
    return found


def code_corpus():
    """Tokens used anywhere, plus runtime-built class prefixes.

    A prefix only protects classes in the stylesheet of the component that
    builds it (Foo.razor -> Foo.razor.scss), unless it is built in plain C# or
    in a component with no stylesheet of its own — then it counts everywhere.
    (Before, AchievementPanel's `cb-@version` made every `cb-*` class in
    GameHUD's sheet look used, hiding the whole retired command bar.)"""
    parts = []
    global_prefixes: set[str] = set()
    scoped: dict[str, set[str]] = {}   # stylesheet stem -> prefixes
    for p in CODE.rglob("*"):
        if p.suffix not in (".razor", ".cs") or not p.is_file():
            continue
        text = p.read_text(encoding="utf-8", errors="replace")
        parts.append(text)
        pre = _prefixes_in(text)
        if not pre:
            continue
        own_sheet = p.with_name(p.name + ".scss") if p.suffix == ".razor" else None
        if own_sheet is not None and own_sheet.exists():
            scoped.setdefault(own_sheet.name, set()).update(pre)
        else:
            global_prefixes |= pre
    corpus = "\n".join(parts)
    tokens = set(re.findall(r"[A-Za-z_][\w-]*", corpus))
    return tokens, (global_prefixes, scoped)


def dead_classes(scss_path: Path, tokens: set[str], prefixes) -> list[str]:
    global_prefixes, scoped = prefixes
    usable = global_prefixes | scoped.get(scss_path.name, set())
    names = classes_in(scss_path.read_text(encoding="utf-8", errors="replace"))
    dead = []
    for c in sorted(names):
        if c in tokens:
            continue
        if any(c.startswith(p) for p in usable):
            continue
        dead.append(c)
    return dead


def main(argv):
    tokens, prefixes = code_corpus()
    show = "--list" in argv
    targets = [Path(a) for a in argv if a.endswith(".scss")]
    if targets:
        show = True
    else:
        targets = sorted(UI.rglob("*.scss"))

    rows = []
    for t in targets:
        t = t if t.is_absolute() else (ROOT / t)
        total = len(classes_in(t.read_text(encoding="utf-8", errors="replace")))
        dead = dead_classes(t, tokens, prefixes)
        rows.append((len(dead), total, t.relative_to(ROOT).as_posix(), dead))

    rows.sort(reverse=True)
    grand = 0
    for n, total, name, dead in rows:
        if not n:
            continue
        grand += n
        print(f"{n:4} / {total:4} probably-dead classes  {name}")
        if show:
            print("      " + " ".join(dead))
    print(f"\nui_deadcss: {grand} probably-dead classes. Confirm with a grep before deleting.")


if __name__ == "__main__":
    main(sys.argv[1:])

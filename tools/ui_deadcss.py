#!/usr/bin/env python3
"""
Beastborne dead-CSS finder — lists SCSS classes that nothing in the code appears to use.

    python tools/ui_deadcss.py              # summary per stylesheet (most dead first)
    python tools/ui_deadcss.py --list       # also print the class names
    python tools/ui_deadcss.py FILE.scss    # just one stylesheet, with names

Stdlib only, no AI. A class counts as USED if its name appears anywhere in Code/**/*.razor
or *.cs (s&box stylesheets are global, so any panel can use any class), or if it starts
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
_CLASS = re.compile(r"(?<![\w-])\.(-?[A-Za-z_][\w-]*)")
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


def code_corpus():
    parts = []
    for p in CODE.rglob("*"):
        if p.suffix in (".razor", ".cs") and p.is_file():
            parts.append(p.read_text(encoding="utf-8", errors="replace"))
    corpus = "\n".join(parts)
    tokens = set(re.findall(r"[A-Za-z_][\w-]*", corpus))
    prefixes = set()
    # class="foo-@x", class="foo-@(x)"
    prefixes |= set(re.findall(r"([A-Za-z_][\w-]*-)@", corpus))
    # "foo-" + x   /   $"foo-{x}"
    prefixes |= set(re.findall(r"\"([A-Za-z_][\w-]*-)\"\s*\+", corpus))
    prefixes |= set(re.findall(r"\$\"[^\"]*?([A-Za-z_][\w-]*-)\{", corpus))
    return tokens, prefixes


def dead_classes(scss_path: Path, tokens: set[str], prefixes: set[str]) -> list[str]:
    names = classes_in(scss_path.read_text(encoding="utf-8", errors="replace"))
    dead = []
    for c in sorted(names):
        if c in tokens:
            continue
        if any(c.startswith(p) for p in prefixes):
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

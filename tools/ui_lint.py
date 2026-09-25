#!/usr/bin/env python3
"""
Beastborne UI lint — catches s&box CSS/Razor patterns we've already learned break.

No AI, no dependencies (stdlib only). Scans Code/UI/**/*.scss, *.razor, *.cs.

    python tools/ui_lint.py                  # report NEW issues vs the baseline (exit 1 on new errors)
    python tools/ui_lint.py --all            # list every issue, baseline included
    python tools/ui_lint.py --update-baseline  # accept the current state as the baseline
    python tools/ui_lint.py --rules          # list rules and why they exist

The baseline (tools/ui_lint_baseline.json) records how many hits each (file, rule) pair
had when it was last accepted, so legacy code doesn't drown out regressions. A file only
reports when its count for a rule goes UP. When you clean legacy hits up, run
--update-baseline so the lower count becomes the new ceiling.

Rules come from CLAUDE.md's quirks table + .claude/ui-knowledge/laws.md. When the engine
changes (see sbox-26-09-changes.md) and a rule is verified obsolete, delete it here too.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
UI_DIR = ROOT / "Code" / "UI"
BASELINE = Path(__file__).resolve().parent / "ui_lint_baseline.json"

# (id, severity, applies-to, regex, why)
#   severity: "error" = known to break; "warn" = risky / unverified on the 26.09 engine
#   applies-to: "css" = .scss and inline style="" in .razor; "code" = razor/C# code
RULES = [
    ("animation-delay", "error", "css", r"\banimation-delay\s*:",
     "Panel re-renders cancel pending delays. Bake staggers into keyframe percentages (CLAUDE.md Animation)."),
    ("unitless-line-height", "error", "css", r"\bline-height\s*:\s*(?:[4-9]|\d{2,})(?:\.\d+)?\s*(?:;|$|!)",
     "Unitless line-height is a font-size MULTIPLIER since 26.06.03 — use px (e.g. 24px)."),
    ("object-position", "error", "css", r"\bobject-position\s*:",
     "object-position doesn't exist in s&box — position the wrapper instead."),
    ("css-var", "error", "css", r"var\(\s*--|^\s*--[\w-]+\s*:",
     "CSS custom properties aren't supported — use SCSS $vars or C#-built inline styles."),
    ("color-mix", "error", "css", r"\bcolor-mix\(",
     "color-mix() isn't supported — build the color in C# (Hex2Rgba)."),
    ("of-type", "error", "css", r":(?:first|last|nth|only)-of-type",
     ":*-of-type is unsupported — use :first-child / :last-child / :nth-child()."),
    ("focus-within", "error", "css", r":focus-within",
     ":focus-within is unsupported."),
    ("repeating-gradient", "error", "css", r"repeating-(?:linear|radial)-gradient\(",
     "repeating-*-gradient() is unsupported — use a baked texture."),
    ("inline-flex", "error", "css", r"\bdisplay\s*:\s*inline-flex",
     "inline-flex is unsupported — use display: flex."),
    ("box-sizing", "error", "css", r"\bbox-sizing\s*:",
     "box-sizing is not a property in s&box (it's ignored)."),
    ("filter-opacity", "error", "css", r"\bfilter\s*:[^;]*\bopacity\(",
     "opacity() is not a filter function — it kills the whole filter declaration."),
    ("url-quotes-inline", "warn", "inline", r"url\(\s*['\"]\s*@",
     "Inline url() with quotes around a Razor value — use url(@var)."),
    ("img-filter", "warn", "css-selector-img", r"\bfilter\s*:\s*(?!none)",
     "filter on <img> blurs pixel art (off-screen composite) — use opacity or a wrapper overlay."),
    ("translate-center", "warn", "css", r"\btransform\s*:[^;]*translate\(\s*-50%\s*,\s*-50%",
     "translate(-50%,-50%) centering poisoned flex-grow on Yoga (re-test on 26.09) — prefer flex centering."),
    ("transparent-gradient", "warn", "css", r"gradient\([^;]*\btransparent\b",
     "'transparent' in gradients failed pre-26.09 — use rgba(...,0) until verified."),
    ("radial-shape", "warn", "css", r"radial-gradient\(\s*(?:circle|ellipse)",
     "radial-gradient shape keywords failed pre-26.09 — use bare percent stops until verified."),
    ("display-block-grid", "warn", "css", r"\bdisplay\s*:\s*(?:block|grid|inline)\b",
     "display: block/grid is new in 26.09 — unverified in our editor; flex is the proven path."),
    ("position-fixed", "warn", "css", r"\bposition\s*:\s*fixed\b",
     "position: fixed is new in 26.09 — unverified; absolute against a full-screen root is proven."),
    ("accepts-focus", "error", "code", r"\bAcceptsFocus\s*=\s*true",
     "A focused panel swallows every Input.Pressed hotkey game-wide (see UiFocusGuard)."),
    ("onmouseenter", "error", "code", r"\bonmouseenter\s*=",
     "onmouseenter isn't supported — use onmouseover with a guard."),
    ("escape-bind", "warn", "code", r"Input\.Pressed\(\s*\"Escape\"\s*\)",
     "Don't add new Escape handlers — Q (\"Menu\") is the back key; s&box reserves Escape."),
    ("globalframe-hash", "warn", "code", r"BuildHash[^\n]*GlobalFrame|GlobalFrame[^\n]*BuildHash",
     "SpriteAnimator.GlobalFrame in BuildHash re-renders the whole panel every sprite tick."),
]

_SCSS_BLOCK_COMMENT = re.compile(r"/\*.*?\*/", re.S)
_RAZOR_BLOCK_COMMENT = re.compile(r"@\*.*?\*@", re.S)
_STYLE_ATTR = re.compile(r"style\s*=\s*\"([^\"]*)\"")


def _blank_keep_lines(m: re.Match) -> str:
    return "\n" * m.group(0).count("\n")


def _strip_line_comment(line: str) -> str:
    # Drop `// ...` unless it's inside url(...) or a string (good enough for our files).
    idx = line.find("//")
    while idx != -1:
        before = line[:idx]
        if before.count('"') % 2 == 0 and before.count("'") % 2 == 0 and "url(" not in before[-12:] and not before.endswith(":"):
            return before
        idx = line.find("//", idx + 2)
    return line


def scan_file(path: Path):
    text = path.read_text(encoding="utf-8", errors="replace")
    ext = path.suffix
    if ext == ".scss":
        text = _SCSS_BLOCK_COMMENT.sub(_blank_keep_lines, text)
    elif ext == ".razor":
        text = _RAZOR_BLOCK_COMMENT.sub(_blank_keep_lines, text)
        text = _SCSS_BLOCK_COMMENT.sub(_blank_keep_lines, text)

    lines = text.split("\n")
    hits = []
    selector_stack: list[str] = []
    pending_selector = ""

    for n, raw in enumerate(lines, 1):
        line = _strip_line_comment(raw)
        stripped = line.strip()

        if ext == ".scss":
            # Track the selector context (for the <img>-filter rule).
            if "{" in line:
                pending_selector += " " + line.split("{")[0]
                for _ in range(line.count("{")):
                    selector_stack.append(pending_selector.strip())
                    pending_selector = ""
            elif stripped and not stripped.endswith(";") and "}" not in line:
                pending_selector += " " + stripped
            in_img = any(re.search(r"(^|[\s>+~,(])img\b", s) for s in selector_stack)

            for rid, sev, kind, pat, _ in RULES:
                if kind == "css" and re.search(pat, line):
                    hits.append((rid, sev, n, stripped))
                elif kind == "css-selector-img" and in_img and re.search(pat, line):
                    hits.append((rid, sev, n, stripped))

            for _ in range(line.count("}")):
                if selector_stack:
                    selector_stack.pop()
            if "}" in line:
                pending_selector = ""

        else:  # .razor / .cs
            if ext == ".razor":
                for m in _STYLE_ATTR.finditer(line):
                    style = m.group(1)
                    for rid, sev, kind, pat, _ in RULES:
                        if kind in ("css", "inline") and re.search(pat, style):
                            hits.append((rid, sev, n, stripped))
            for rid, sev, kind, pat, _ in RULES:
                if kind == "code" and re.search(pat, line):
                    hits.append((rid, sev, n, stripped))
    return hits


def collect():
    results = {}
    for path in sorted(UI_DIR.rglob("*")):
        if path.suffix not in (".scss", ".razor", ".cs") or not path.is_file():
            continue
        hits = scan_file(path)
        if hits:
            results[path.relative_to(ROOT).as_posix()] = hits
    return results


def counts_of(results):
    out = defaultdict(dict)
    for f, hits in results.items():
        for rid, c in Counter(h[0] for h in hits).items():
            out[f][rid] = c
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--all", action="store_true", help="list every issue, ignoring the baseline")
    ap.add_argument("--update-baseline", action="store_true", help="accept current counts as the baseline")
    ap.add_argument("--rules", action="store_true", help="print the rules and why they exist")
    args = ap.parse_args()

    if args.rules:
        for rid, sev, kind, _, why in RULES:
            print(f"{sev.upper():5}  {rid:22} {why}")
        return 0

    results = collect()
    counts = counts_of(results)

    if args.update_baseline:
        BASELINE.write_text(json.dumps(counts, indent=1, sort_keys=True) + "\n", encoding="utf-8")
        total = sum(sum(v.values()) for v in counts.values())
        print(f"Baseline updated: {total} legacy hits across {len(counts)} files -> {BASELINE.relative_to(ROOT)}")
        return 0

    baseline = {}
    if BASELINE.exists() and not args.all:
        baseline = json.loads(BASELINE.read_text(encoding="utf-8"))

    sev_of = {r[0]: r[1] for r in RULES}
    why_of = {r[0]: r[4] for r in RULES}
    new_errors = new_warns = 0
    shown_rules = set()

    for f, hits in results.items():
        by_rule = defaultdict(list)
        for h in hits:
            by_rule[h[0]].append(h)
        for rid, rh in by_rule.items():
            allowed = baseline.get(f, {}).get(rid, 0)
            if len(rh) <= allowed:
                continue
            extra = len(rh) - allowed
            if sev_of[rid] == "error":
                new_errors += extra
            else:
                new_warns += extra
            shown_rules.add(rid)
            label = "" if args.all or not allowed else f"  (+{extra} over baseline {allowed}; showing all {len(rh)})"
            print(f"{sev_of[rid].upper()} {rid}  {f}{label}")
            for _, _, n, src in rh:
                print(f"    {f}:{n}: {src[:140]}")

    if shown_rules:
        print("\nWhy:")
        for rid in sorted(shown_rules):
            print(f"  {rid}: {why_of[rid]}")

    scope = "total" if args.all or not baseline else "new (vs baseline)"
    print(f"\nui_lint: {new_errors} error(s), {new_warns} warning(s) {scope}.")
    return 1 if new_errors else 0


if __name__ == "__main__":
    sys.exit(main())

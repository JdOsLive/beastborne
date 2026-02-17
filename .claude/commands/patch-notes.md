Generate Discord patch notes for Beastborne.

1. Run `git log --oneline -20` to see recent commits
2. Read the changed files to understand what was actually modified
3. Group changes by what matters most to players — lead with the biggest gameplay impact

Output the patch notes inside a single markdown code block (```...```) so the user can copy-paste it directly into Discord.

Format as Discord markdown. Use this structure as a guide, but adapt it naturally to fit the actual changes:

```
# 🎮 BEASTBORNE [VERSION] — [Update Name]

[Short punchy summary of what this update brings]

---

## [Emoji] [Feature Name]
[What changed and why it matters, written like a dev talking to their players]

- **[Detail]** — [What it does]

## 🔧 Fixes & Improvements
- [Fix or change, written plainly]

---

*Thanks for playing Beastborne! Drop feedback in #suggestions* 🐉
```

Writing guidelines:
- Write like a developer, not a marketing team. Casual but clear.
- One emoji per section header max. Skip emojis entirely if it reads better without them.
- Don't start every bullet with "Added" / "Improved" / "Fixed" — vary it. Say what changed.
- Keep bullets to one line. If it needs more, it should be its own section.
- Don't oversell small changes. A bug fix is a bug fix, not a "quality of life enhancement."
- Balance changes should include the actual numbers when relevant.

Ask me for the version number and update name if not obvious from the commits.

$ARGUMENTS

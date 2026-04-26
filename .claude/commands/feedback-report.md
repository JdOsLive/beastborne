Pull the current Beastborne community feedback state from the live API and report it.

## What to do

1. Hit the export endpoint with admin headers:

```bash
curl -s "http://157.245.10.193.nip.io:3000/api/feedback/export" \
  -H "X-API-Key: 5ff1f572c6f9a9d78df95bf152a57aeb5558074b503601ce22ff6f20bdf954a4" \
  -H "X-Steam-Id: 76561198088759073"
```

If the user passes `include_resolved` as an argument (or asks for resolved entries too), append `?include_resolved=true` to the URL.

2. Parse the JSON response. Schema:

```json
{
  "exported_at": "2026-04-20T...",
  "counts": { "bugs": N, "suggestions": N, "resolved": N },
  "bugs": [ { id, title, body, tag, votes, author_name, created_at, ... }, ... ],
  "suggestions": [ ... same shape ... ],
  "resolved": [ ... only if include_resolved=true ... ]
}
```

3. Render a concise terminal-friendly report:

```
🐛 BUGS — N open, M resolved
─────────────────────────────────────
#42 (12 votes) · fusion · 3d ago · @somePlayer
   Fusion popup soft-locks on cancel
   What happened: clicked Fuse, popup opened, clicked Cancel...
   Steps: 1. Open Breeding 2. Pick two monsters...

#37 (7 votes) · battle · 1d ago · @JdOs
   ...

💡 IDEAS — N
─────────────────────────────────────
#8 (5 votes) · monster · 4d ago · @somePlayer
   Add a favourites tab to shop
   ...

✅ Resolved (N total) — pass include_resolved=true to see them
```

4. **Sort within each section by votes desc, then by created_at desc** (matches the Top sort in-game). Show the **top 10 bugs and top 10 ideas** by default, more if there are fewer than 10 total.

5. **Format dates as relative** ("3d ago", "1h ago", "just now") — converts `created_at` (UTC ISO timestamp) to relative.

6. **For the bug body**, parse out the structured sections — body is markdown-formatted by the in-game composer with `**Game Version:** ...`, `**What Happened:**`, `**Steps to Reproduce:**`, `**Expected:**`, `**Actual:**`. Show these as labeled lines, NOT the raw markdown.

7. **Trim long bodies** to ~300 chars per entry in the report. Show "(truncated)" if cut.

8. **Highlight notable ones** with a marker:
   - 🔥 if votes ≥ 5
   - ⚡ if `crash` tag
   - ⏰ if older than 7 days and unresolved (stale)

## End-of-report summary

After listing, give a short triage paragraph:
- Most-voted open bug
- Most-voted open idea
- Anything tagged `crash`
- How many are stale (>7 days unresolved)

## Failure modes

- API unreachable → say so, suggest `pm2 status` on the droplet
- Empty arrays → say "No open bugs" / "No open ideas" with the absolute counts
- 403 → admin allowlist may have changed (env var on droplet)

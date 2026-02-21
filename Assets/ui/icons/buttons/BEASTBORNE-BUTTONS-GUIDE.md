# Beastborne UI Buttons — Claude Code Implementation Guide

## Icons

14 standalone SVGs in `ui/icons/ui/`. Transparent background, 24x24 viewBox.

| Icon | File | Color | Shape |
|------|------|-------|-------|
| Menu | `menu.svg` | White | 3 horizontal bars |
| Inventory | `inventory.svg` | #ef4444 | Satchel with handle + flap + clasp |
| Chat | `chat.svg` | #7c3aed | Speech bubble with 3 dots |
| Effects | `effects.svg` | #eab308 | 4-point sparkle + mini sparkle |
| Music | `music.svg` | #c084fc | Double music note |
| Play | `play.svg` | White | Play triangle |
| Pause | `pause.svg` | White | Two vertical bars |
| Skip Forward | `skip-forward.svg` | White | Triangle + end bar |
| Skip Back | `skip-back.svg` | White | End bar + reverse triangle |
| Settings | `settings.svg` | White | Gear with center hole |
| Notification | `notification.svg` | #f59e0b | Bell |
| Back | `back.svg` | White | Left chevron |
| Close | `close.svg` | White | X |
| Info | `info.svg` | White | Circle with i |

---

## Backgrounds

Button backgrounds for each size and state. These are 9-slice-ready rounded rects.

| File | Size | Use For |
|------|------|---------|
| `btn-default.svg` | 200×60 rx14 | Menu, Inventory (default) |
| `btn-default-hover.svg` | 200×60 rx14 | Menu, Inventory (hovered) |
| `btn-default-pressed.svg` | 200×60 rx14 | Menu, Inventory (pressed) |
| `btn-default-active.svg` | 200×60 rx14 | Any button (selected/active) |
| `btn-small.svg` | 160×48 rx12 | Chat, Effects, Music, Settings, etc (default) |
| `btn-small-hover.svg` | 160×48 rx12 | Small buttons (hovered) |
| `btn-small-pressed.svg` | 160×48 rx12 | Small buttons (pressed) |
| `btn-icon.svg` | 48×48 rx12 | Play, Pause, Skip, Back, Close, Info (default) |
| `btn-icon-hover.svg` | 48×48 rx12 | Icon-only buttons (hovered) |
| `btn-icon-pressed.svg` | 48×48 rx12 | Icon-only buttons (pressed) |
| `bottom-bar.svg` | 800×80 | Bottom bar gradient bg (stretches) |
| `top-bar.svg` | 800×52 | Top bar bg (stretches) |

### Background Colors
```
Default:  #252530  border rgba(255,255,255,0.06)
Hover:    #2e2e3e  border rgba(255,255,255,0.12)
Pressed:  #222230  border rgba(255,255,255,0.06)
Active:   #2a2840  border rgba(124,58,237,0.3)
```

If s&box supports CSS background-color natively, you can skip the background SVGs and just use the SCSS colors. If you need image-based backgrounds (e.g. for 9-slice panels), use these SVGs.

---

## File Structure

```
ui/
├── Components/
│   ├── GameButton.razor
│   ├── GameButton.razor.scss
│   ├── BottomBar.razor
│   ├── BottomBar.razor.scss
│   ├── TopBar.razor
│   └── TopBar.razor.scss
└── icons/
    └── ui/
        ├── menu.svg
        ├── inventory.svg
        ├── chat.svg
        ├── effects.svg
        ├── music.svg
        ├── play.svg
        ├── pause.svg
        ├── skip-forward.svg
        ├── skip-back.svg
        ├── settings.svg
        ├── notification.svg
        ├── back.svg
        ├── close.svg
        ├── info.svg
        └── backgrounds/
            ├── btn-default.svg
            ├── btn-default-hover.svg
            ├── btn-default-pressed.svg
            ├── btn-default-active.svg
            ├── btn-small.svg
            ├── btn-small-hover.svg
            ├── btn-small-pressed.svg
            ├── btn-icon.svg
            ├── btn-icon-hover.svg
            ├── btn-icon-pressed.svg
            ├── bottom-bar.svg
            └── top-bar.svg
```

---

## GameButton Component

s&box Razor UI supports CSS `transition` on properties like `transform`, `opacity`, `background-color`, and `box-shadow`. Animations are done by toggling CSS classes from C# code — the transitions handle the interpolation.

### GameButton.razor

```razor
<div class="game-btn @SizeClass @StateClass @HoverClass"
     onmouseenter="@OnHoverStart"
     onmouseleave="@OnHoverEnd"
     onmousedown="@OnPressStart"
     onmouseup="@OnPressEnd"
     onclick="@OnClick">

    <div class="game-btn-icon">
        <img src="/ui/icons/ui/@(Icon).svg" />
    </div>

    @if (!string.IsNullOrEmpty(Label))
    {
        <span class="game-btn-label">@Label</span>
    }

    @if (BadgeCount > 0)
    {
        <span class="game-btn-badge" style="background-color: @BadgeColor;">@BadgeCount</span>
    }
</div>

@code {
    [Parameter] public string Icon { get; set; } = "menu";
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public int BadgeCount { get; set; } = 0;
    [Parameter] public string BadgeColor { get; set; } = "#7c3aed";
    [Parameter] public bool Small { get; set; } = false;
    [Parameter] public bool IconOnly { get; set; } = false;
    [Parameter] public bool Active { get; set; } = false;
    [Parameter] public bool Disabled { get; set; } = false;
    [Parameter] public Action OnClick { get; set; }

    private bool IsHovered { get; set; } = false;
    private bool IsPressed { get; set; } = false;

    private string SizeClass => Small ? "game-btn--sm" : (IconOnly ? "game-btn--icon-only" : "");
    private string StateClass =>
        Disabled ? "game-btn--disabled" :
        Active ? "game-btn--active" : "";
    private string HoverClass =>
        IsPressed ? "game-btn--pressed" :
        IsHovered ? "game-btn--hovered" : "";

    private void OnHoverStart() => IsHovered = true;
    private void OnHoverEnd() { IsHovered = false; IsPressed = false; }
    private void OnPressStart() => IsPressed = true;
    private void OnPressEnd() => IsPressed = false;
}
```

### GameButton.razor.scss

```scss
.game-btn {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 12px 24px 12px 18px;
    background-color: #252530;
    border-radius: 14px;
    cursor: pointer;
    border: 1.5px solid rgba(255, 255, 255, 0.06);

    // s&box transition — these animate when classes toggle
    transition: transform 0.2s ease,
                background-color 0.2s ease,
                border-color 0.2s ease,
                box-shadow 0.2s ease;

    // ===== HOVER =====
    &--hovered {
        background-color: #2e2e3e;
        border-color: rgba(255, 255, 255, 0.12);
        transform: translateY(-2px);
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);

        .game-btn-icon {
            transform: scale(1.08);
        }
        .game-btn-label {
            color: #d0d0e4;
        }
        .game-btn-badge {
            transform: scale(1.15);
        }
    }

    // ===== PRESSED =====
    &--pressed {
        transform: translateY(0px) scale(0.97);
        box-shadow: none;
        transition-duration: 0.08s;

        .game-btn-icon {
            transform: scale(0.95);
        }
    }

    // ===== ACTIVE =====
    &--active {
        background-color: #2a2840;
        border-color: rgba(124, 58, 237, 0.3);

        .game-btn-label {
            color: #c4b5fd;
        }
    }

    // ===== DISABLED =====
    &--disabled {
        opacity: 0.4;
        pointer-events: none;
    }

    // ===== SIZES =====
    &--sm {
        padding: 9px 16px 9px 12px;
        border-radius: 12px;
        gap: 8px;

        .game-btn-icon { width: 20px; height: 20px; }
        .game-btn-label { font-size: 11px; }
    }

    &--icon-only {
        padding: 10px;
        gap: 0;
        &.game-btn--sm { padding: 8px; }
    }

    // ===== CHILDREN =====
    &-icon {
        width: 24px;
        height: 24px;
        flex-shrink: 0;
        transition: transform 0.25s ease;
        img { width: 100%; height: 100%; }
    }

    &-label {
        font-family: 'Exo 2', sans-serif;
        font-weight: 800;
        font-size: 13px;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        color: #8888a8;
        transition: color 0.2s ease;
        white-space: nowrap;
    }

    &-badge {
        color: white;
        font-size: 10px;
        font-weight: 800;
        min-width: 18px;
        height: 18px;
        border-radius: 9px;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 0 5px;
        transition: transform 0.2s ease;
    }
}
```

---

## BottomBar

### BottomBar.razor

```razor
<div class="bottom-bar">
    <div class="bottom-bar-left">
        <GameButton Icon="chat" Label="Chat" Small="true"
                    BadgeCount="@ChatCount" BadgeColor="#7c3aed"
                    OnClick="@ToggleChat" />
    </div>

    <div class="bottom-bar-center">
        <GameButton Icon="menu" Label="Menu" OnClick="@OpenMenu" />
        <GameButton Icon="inventory" Label="Inventory" OnClick="@OpenInventory" />
    </div>

    <div class="bottom-bar-right">
        <GameButton Icon="effects" Label="Effects" Small="true"
                    BadgeCount="@EffectsCount" BadgeColor="#eab308"
                    OnClick="@ToggleEffects" />
    </div>
</div>

@code {
    [Parameter] public int ChatCount { get; set; } = 0;
    [Parameter] public int EffectsCount { get; set; } = 0;
    [Parameter] public Action ToggleChat { get; set; }
    [Parameter] public Action OpenMenu { get; set; }
    [Parameter] public Action OpenInventory { get; set; }
    [Parameter] public Action ToggleEffects { get; set; }
}
```

### BottomBar.razor.scss

```scss
.bottom-bar {
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    padding: 12px 24px 16px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(180deg, rgba(14, 14, 24, 0) 0%, rgba(14, 14, 24, 1) 30%);
    pointer-events: none;
    > * { pointer-events: all; }

    &-left, &-right { position: absolute; }
    &-left { left: 24px; }
    &-right { right: 24px; }
    &-center { display: flex; gap: 12px; }
}
```

---

## TopBar Music Player

### TopBar.razor

```razor
<div class="top-bar">
    <GameButton Icon="notification" Label="Notifications" Small="true"
                OnClick="@OpenNotifications" />

    <GameButton Icon="music" Label="@CurrentSong" Small="true"
                OnClick="@ToggleMusicPanel" />

    <div class="top-bar-spacer"></div>

    <div class="music-controls">
        <GameButton Icon="skip-back" IconOnly="true" Small="true"
                    OnClick="@PrevTrack" />
        <GameButton Icon="@(IsPlaying ? "pause" : "play")" IconOnly="true" Small="true"
                    OnClick="@TogglePlayback" />
        <GameButton Icon="skip-forward" IconOnly="true" Small="true"
                    OnClick="@NextTrack" />
    </div>
</div>

@code {
    [Parameter] public string CurrentSong { get; set; } = "Evening Glow";
    [Parameter] public bool IsPlaying { get; set; } = true;
    [Parameter] public Action OpenNotifications { get; set; }
    [Parameter] public Action ToggleMusicPanel { get; set; }
    [Parameter] public Action PrevTrack { get; set; }
    [Parameter] public Action TogglePlayback { get; set; }
    [Parameter] public Action NextTrack { get; set; }
}
```

### TopBar.razor.scss

```scss
.top-bar {
    width: 100%;
    background-color: #12121c;
    border: 1px solid #1e1e2e;
    border-radius: 16px;
    padding: 8px 14px;
    display: flex;
    align-items: center;
    gap: 8px;

    &-spacer { flex: 1; }
}

.music-controls {
    display: flex;
    gap: 4px;
    align-items: center;
}
```

---

## Usage Examples

```razor
// Button with label
<GameButton Icon="menu" Label="Menu" OnClick="@OpenMenu" />

// Small with badge
<GameButton Icon="chat" Label="Chat" Small="true"
            BadgeCount="3" BadgeColor="#7c3aed" />

// Icon only (back, close, play controls)
<GameButton Icon="close" IconOnly="true" OnClick="@ClosePanel" />
<GameButton Icon="play" IconOnly="true" Small="true" OnClick="@Play" />

// Active state
<GameButton Icon="settings" Label="Settings" Active="true" />

// Disabled
<GameButton Icon="menu" Label="Menu" Disabled="true" />

// Music — swap icon based on play state
<GameButton Icon="@(IsPlaying ? "pause" : "play")"
            IconOnly="true" Small="true"
            OnClick="@TogglePlayback" />
```

---

## How Animations Work in s&box

s&box Razor supports `transition` but NOT `@keyframes`. The pattern:

1. Define default styles on the base class
2. Define target styles on modifier classes (`--hovered`, `--pressed`)
3. Put `transition` on the base class
4. Toggle classes from C# via mouse events
5. CSS transitions interpolate between states

```
DEFAULT → mouseenter → HOVERED (lift, glow, icon scales up)
HOVERED → mousedown  → PRESSED (squish down, shadow gone)
PRESSED → mouseup    → HOVERED (spring back)
HOVERED → mouseleave → DEFAULT (everything resets)
```

**Works in s&box:** `transform`, `opacity`, `background-color`, `border-color`, `box-shadow`, `color`

**Avoid:** `width`/`height` (reflow), `@keyframes`/`animation` (not supported), `filter` (may not work)

---

## Colors

```
Button bg:       #252530
Button hover bg: #2e2e3e
Button border:   rgba(255, 255, 255, 0.06)
Border hover:    rgba(255, 255, 255, 0.12)
Label default:   #8888a8
Label hover:     #d0d0e4
Label active:    #c4b5fd
Active bg:       #2a2840
Active border:   rgba(124, 58, 237, 0.3)
Bar bg:          #12121c
Page bg:         #0e0e18
```

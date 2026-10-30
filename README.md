---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 89238829fb97168b9c4195c03c152aef_b37bf107d47d11f1a0da525400393706
    ReservedCode1: H4zknSQkW2A1RVEN6Yzzd2Ws3PyXa5SQ3nQwmh+SAPBKalRQFus9+rQfM87h/QbgY2BDTEX7NOTx7d0ZQIA7gJXGVPqTubNY29rd+h5EYWY0QaGx3/bKJdMAjg2Uih+qE6bEn5llw0RW8U+BeMQiTJgPvbe1+MH2PK9f1cFkI+jWILs0aYDSNht0dzY=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 89238829fb97168b9c4195c03c152aef_b37bf107d47d11f1a0da525400393706
    ReservedCode2: H4zknSQkW2A1RVEN6Yzzd2Ws3PyXa5SQ3nQwmh+SAPBKalRQFus9+rQfM87h/QbgY2BDTEX7NOTx7d0ZQIA7gJXGVPqTubNY29rd+h5EYWY0QaGx3/bKJdMAjg2Uih+qE6bEn5llw0RW8U+BeMQiTJgPvbe1+MH2PK9f1cFkI+jWILs0aYDSNht0dzY=
---

# EcoNotepad — market / shopping notes overlay for economy games

A small always-on-top WinForms notepad for keeping track of what to buy, at what price and from which store while playing economy games (Eco Global Survival, High Seas High Profits, etc.).

It is a **separate floating window** — nothing is injected into the game, so it works with any game.

## Features

- Entries with: **Item, Price, Store, Qty, Tags, Notes** + a **Done** checkbox (for things already purchased).
- **Always on top** — floats above the game.
- **Click-through mode** — mouse input passes through to the game; use it when you just want the notes visible.
- **Opacity cycling** — 100% / 85% / 65%, handy when you want to see the game through the panel.
- **Hide to tray** + global hotkeys, so you can summon it mid-game.
- **Search / filter** across item, price, store, qty, tags and notes.
- Auto-saves to `notes.json` (JSON, next to the exe) — easy to back up or edit by hand.
- Remembers window position, size, opacity and click-through state (`settings.json`).

## Hotkeys (global)

| Hotkey        | Action                   |
| ------------- | ------------------------ |
| Ctrl+Shift+N  | Show / hide the window   |
| Ctrl+Shift+T  | Toggle click-through     |

(These were chosen to avoid AltGr conflicts on Spanish/European keyboard layouts.)

## Build

Requirements: .NET SDK (tested with 10.x) and the .NET Framework 4.8 targeting pack (pre-installed on Windows 11).

```
dotnet build -c Release
```

or simply run `build.bat`. Output: `bin\Release\net48\EcoNotepad.exe`.

## Run

Run `bin\Release\net48\EcoNotepad.exe`. The data files `notes.json` and `settings.json` are created next to the exe; if that folder is not writable they fall back to `%APPDATA%\EcoNotepad`.

## Overlay tip

The window floats above the game only when the game runs in **windowed / borderless-windowed** mode. Exclusive fullscreen blocks external windows. In Eco Global Survival, set the video mode to Windowed or Borderless.

## Project layout

```
EcoNotepad.csproj      SDK-style project (net48, WinForms)
app.manifest           per-monitor V2 DPI awareness
build.bat              convenience build script
src/Program.cs         entry point
src/MainForm.cs        overlay window, grid, hotkeys, click-through
src/EntryDialog.cs     add / edit entry dialog
src/NoteEntry.cs       data model
src/DataStore.cs       JSON load/save (notes + settings)
src/Json.cs            tiny dependency-free JSON reader/writer
src/HotkeyManager.cs   global hotkey registration
```
*（内容由AI生成，仅供参考）*

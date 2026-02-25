# Language Server Script Editor

External Script Editor for Unity that launches a user-defined command and delegates `.sln` / `.csproj` generation to `com.unity.ide.vscode`.

## Features

- Registers as a Unity External Script Editor and runs any command you specify.
- Passes file/line/column to your editor via placeholders in a command template.
- Reuses VS Code’s project generation pipeline and settings UI.

## Install
Unity Package Manager(UPM) support path query parameter of git package. You can add `https://github.com/tanitta/LanguageServerScriptEditor
.git` to Package Manager.

## Setup

1. In Unity, open `Edit > Preferences > External Tools`.
2. Select **Language Server Script Editor**.
3. Configure the **Command Template** field.

The settings UI is provided by this package and includes the same project generation toggles as the VS Code integration.

## Command Template

The template is tokenized like a shell command: quotes keep arguments together, whitespace separates tokens, and no extra escaping is performed.

Supported placeholders:

- `$(File)` — full path to the file, or the project root if no file is provided
- `$(Line)` — 1-based line number (defaults to 1)
- `$(Column)` — 1-based column number (defaults to 1)

Examples:

```
code "$(File)" -g $(Line):$(Column)
```

```
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\path\to\open.ps1" "$(File)" -g $(Line):$(Column)
```

## Project Generation

This package wraps `com.unity.ide.vscode` to:

- generate `.sln` and `.csproj` files
- expose package selection flags (embedded/local/registry/git/built-in/etc.)
- provide a **Regenerate project files** button

If this editor is selected and no solution exists, project files are generated on startup.

## Dependencies

- `com.unity.ide.vscode` (project generation and settings)

## Compatibility

- Unity 2019.4+

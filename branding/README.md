# Branding — make EchoFactory yours

Everything here is **optional** and copied next to the game on build. Drop your
own files in and rebuild. Nothing in this folder is required for the game to run
(missing files fall back to a labelled placeholder).

## 1. Intro logo (shown on launch)

Drop your logo image here as **`logo.png`**:

```
branding/logo.png
```

- PNG, JPG or BMP all work. A transparent PNG looks best.
- Recommended size: roughly **920 × 460 px** (it is scaled to fit, aspect
  preserved, so any size works).
- Until you add it, the intro shows a `[ YOUR LOGO HERE ]` placeholder telling
  you this path.

### Changing the path / studio name / turning the intro off

These live in `echofactory-settings.json` (created next to the game on first
run):

```json
{
  "ShowIntro": true,
  "LogoPath": "branding/logo.png",
  "CompanyName": "YOUR STUDIO"
}
```

- `LogoPath` may be relative (resolved next to the game) or an absolute path.
- `CompanyName` is drawn under the logo; set it to `""` to hide it.
- `ShowIntro: false` skips straight to the main menu.

## 2. Application icon (the .exe icon in Explorer / taskbar)

Drop a Windows icon file here as **`app.ico`**:

```
branding/app.ico
```

Then rebuild. The project picks it up automatically
(`<ApplicationIcon>` is wired conditionally, so the build still works when it is
absent). Use a multi-resolution `.ico` (16/32/48/256 px) for crisp results.

To make an `.ico` from a PNG you can use any online converter or, with
ImageMagick:

```
magick logo.png -define icon:auto-resize=256,48,32,16 app.ico
```

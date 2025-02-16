# Fluent-Gmail
Fluent Gmail is a wrapper written in WPF for Gmail that allows you to enhance your experience by introducing Microsoft's Fluent design to the app. The project matches your system's native look and feel, with utilizing mica material. Also transfers some functionality from page to native app.

Each feature in the project is called an integration. Integrations can fix styling and create or move functionality. Most of them are opt-out with a config file.

## Downloading
Downloads are not yet available. Once it's released, you can go to Releases page or get latest Actions build.

## Installation
Not released yet. Try to compile from source. You are on your own, good luck.

## Custom Styles
You can inject custom styles into your Gmail instance. Create a `Custom.css` inside installation location / working directory and put your stylesheet inside. It will automatically gets loaded on startup. Also, all styles gets elevated with `!important` tag to override default styles easily.

## Configuring
Create a `config.ini` file inside installation location / working directory. Here is a template config file with all available options to start with:
```ini
# Moves account popup to top of the page
# Default: true
fix-account-popup=true

# Aligns filter box with search box
# Default: true
fix-filter-box=true

# Sets all internal (in-browser) gaps to 4
# Default: true
fix-gaps=true

# Enables mica
# Default: true
mica=true

# Enabled mica on login screen
# Default: true
mica-login=true

# Moves header button to native title bar
# Default: true
native-controls=true

# Shows Gmail branding logo on native title bar
# Default: true
native-logo=true

# WIP - smoothly reveals window contents after loading finished
# Default: true
smooth-reveal=true

# Mouse scrol on titlebar to maximize, normalize and minimize
# Default: true
wheel-window-state=true

# Disable MS Edge context menu
# Default: true
disable-system-context=true

# Enable F12 DevTools
# Default: true
devtools=true
```

## Screenshots
Screenshots will the provided with first stable release. Hint: A native looking Gmail interface with Mica material in the back.

## Roadmap
- [x] Editable external CSS
- [x] Make Mica integrations opt-outable (GUI or config file)
- [x] Edit ReadMe
- [ ] Add screenshots
- [ ] Add CI/CD
- [ ] Create packaging workflow
- [ ] Implement smooth reveal
- [ ] Release
- [ ] Make external styles opt-out with config file

## Contributing and Support

Any PR and issue is really appreciated. You can show your support by ⭐ starring the repo.

## Credits

Fluent Gmail is created and maintained by [@Segilmez06](https://github.com/Segilmez06).

Frameworks and libraries used in this project:
- [Microsoft Edge WebView 2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/) by [Microsoft](https://github.com/microsoft) - WebView for displaying Gmail page
- [WPF-UI](https://github.com/lepoco/wpfui) by [Lepo.co](https://github.com/lepoco) - Awesome Fluent design library for WPF
- [LottieSharp](https://github.com/quicoli/LottieSharp) by [@quicoli](https://github.com/quicoli) - Lottie Animation viewer for WPF
- [Costura.Fody](https://github.com/Fody/Costura) by [Fody](https://github.com/Fody) - For embedding assemblies inside main executable
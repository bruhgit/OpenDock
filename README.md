# OpenDock 🚀

A lightweight, high-performance, and modern **macOS-style Dock for Windows 11 and 10**. OpenDock is fully open source, highly customizable, and written in C# using native APIs (.NET / WinForms) with no heavy external dependencies.

---

## 🌟 Key Features

### 1. 🖥️ Modern macOS Dock Experience
* **Aero Acrylic & Glassmorphism**: Provides a premium semi-transparent frosted-glass aesthetic with customizable background opacity.
* **Active Window Tracking**: Real-time window capture showcasing active and minimized applications with elegant dot indicators underneath, just like macOS.
* **Dynamic Icon Resizing**: Supports icon scales ranging from 16px to 128px.
* **Custom Logo Launcher**: Upload your own custom `32x32` pixel image for the start menu button or reset to the Windows default icon with one click.

### 2. 🕒 Dynamic Clock Widget with CSS Engines
* **Style via CSS**: Configure fonts, text alignments, shadows, sizes, and colors of your clock widget using a `opendock.css` file.
* **Hot-Reloading (FileSystemWatcher)**: Any saved changes in `opendock.css` are instantly hot-reloaded and rendered on your clock without restarting the application.
* **Modern Typography Support**: Easily apply premium web/system fonts (e.g., Inter, Segoe UI, Outfit) using regular CSS rules.

### 3. 🌀 Immersive 3D Carousel Switcher (3D Virtual Desktops)
* **Infinite Carousel Rotation**: Rotate the 3D desktop carousel infinitely horizontally without boundary locks.
* **Drag-to-Rotate / Click-to-Select Split**: Dragging and releasing snaps the carousel to the nearest workspace face but **keeps the transition overlay open** so you can preview workspaces. Clicking a desktop face instantly switches to that desktop and closes the carousel.
* **Interruptible Snap Physics (Flick Gestures)**: Mid-snap animations are completely interruptible. Grab or click the carousel at any moment to take control of rotation immediately.
* **Advanced Camera & Overlay Customization**: Adjust FOV (Zoom factor), camera Z depth, overlay pre-capture limits, and transition deceleration coefficients straight from the tray menu.
* **Starry Night Background**: Determinisitic and flicker-free sky star generator that renders beautiful background particles without causing GPU spikes.
* **Pre-cached Custom Backgrounds**: Choose a transition background image or fall back to the default dark blue space gradient. The background image is pre-cached on startup for zero blitting latency.

### 4. 🌍 100 Languages Support & Auto-detection
* **Automatic Locale Detection**: Automatically queries your active Windows UI language (`CultureInfo.CurrentUICulture`) on startup and applies the correct translation.
* **100 Languages Built-in**: Full and partial dictionary mappings for **100 world languages** (English, Turkish, Spanish, French, German, Russian, Chinese, Japanese, Italian, Portuguese, Arabic, Hindi, and more) compiled cleanly inside a standalone [`Languages.cs`](file:///c:/Users/User/source/repos/OpenDock/OpenDock/Languages.cs) file. Fallbacks safely to English.

### 5. 🎮 Game Mode Resource Optimizer
* Hides the dock automatically and sleeps all transition rendering timers when a full-screen game or application is focused, ensuring maximum frame rate and zero CPU/GPU overhead.

---

## 🛠️ Building and Running

OpenDock relies entirely on native Win32/COM/DWM APIs. You do not need to install complex framework packages to compile it.

1. Clone or download the repository.
2. Open `OpenDock.sln` inside **Visual Studio** (2019, 2022 or higher).
3. Set your build configuration to `Release` (or `Debug`) and Target CPU to `AnyCPU` or `x64`.
4. Press `F5` to compile and launch.

---

## ⚙️ Configuration & Customization

* **Tray System**: Right-click the OpenDock taskbar tray icon to access opacity scales, icon metrics, transition settings, startup automation, and custom layouts.
* **CSS Customization**: Open `opendock.css` (generated in the application directory) to adjust clock styles:
```css

/* OpenDock Style Sheet */

.dock {
    /* Background color of the dock (Hex, RGB or RGBA for transparency) */
    background-color: rgba(18, 18, 20, 0.3);

    /* Border color of the dock */
    border-color: rgba(255, 255, 255, 0.15);

    /* Border line width in pixels */
    border-width: 1px;

    /* Corner rounding radius in pixels */
    border-radius: 16px;

    /* Spacing between icons in pixels */
    icon-spacing: 15px;

    /* Magnified icon size when hovered (in pixels, 48 - 128) */
    magnification-size: 48px;
}

.clock {
    font-family: 'Segoe UI';
    font-size: 24px;
    font-weight: bold;
    color: #ffffff;
    text-align: right;
}

```

---

## 📝 License

This project is licensed under the **MIT License**. Feel free to use, modify, and distribute it as you see fit.

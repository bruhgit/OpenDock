const { app, BrowserWindow, ipcMain, dialog, shell } = require('electron');
const path = require('path');
const fs = require('fs');
const os = require('os');
const { spawn, exec } = require('child_process');

let mainWindow = null;

// Paths for OpenDock settings and executable
const localAppData = process.env.LOCALAPPDATA || path.join(os.homedir(), 'AppData', 'Local');
const settingsDir = path.join(localAppData, 'OpenDock');
const settingsPath = path.join(settingsDir, 'opendock_settings.json');

// Paths where OpenDock executable might reside in repository
const repoRoot = path.resolve(__dirname, '..');
const possibleExePaths = [
  path.join(repoRoot, 'OpenDock', 'bin', 'Debug', 'net9.0-windows10.0.17763.0', 'OpenDock.exe'),
  path.join(repoRoot, 'OpenDock', 'bin', 'Release', 'net9.0-windows10.0.17763.0', 'OpenDock.exe'),
  path.join(repoRoot, 'OpenDock', 'bin', 'Debug', 'net8.0-windows10.0.17763.0', 'OpenDock.exe'),
  path.join(settingsDir, 'OpenDock.exe')
];

// Locations for dock_pins.json
const possiblePinsPaths = [
  path.join(settingsDir, 'dock_pins.json'),
  path.join(repoRoot, 'OpenDock', 'bin', 'Debug', 'net9.0-windows10.0.17763.0', 'dock_pins.json'),
  path.join(repoRoot, 'OpenDock', 'dock_pins.json')
];

const defaultSettings = {
  DockColorArgb: -1363652072, // 175, 24, 24, 24
  MenuColorArgb: -1363652072,
  SearchColorArgb: -13816531, // 45, 45, 45
  MenuLogoPath: '',
  DockPosition: 'Bottom',
  GameModeEnabled: false,
  ShowClock: true,
  TransitionFocalScale: 2.8,
  TransitionCameraOffset: -4.5,
  TransitionOverlayPrecaptureDepth: 1,
  TransitionDeceleration: 0.15,
  TransitionSlicesCount: 50,
  DockIconSize: 32,
  DockLength: 640,
  DockBackgroundAlpha: 175,
  TransitionShowStars: true,
  TransitionStarsCount: 20,
  TransitionOverlapPadding: 1.5,
  TransitionSwitchDelay: 100,
  TransitionBackgroundImagePath: '',
  TransitionOutwardFacing: false,
  SeparateRunningApps: true,
  AutoHideEnabled: false,
  ShowRecycleBin: true,
  MagnificationEnabled: true,
  MagnifiedIconSize: 64,
  MagnificationRange: 3,
  ShowMediaController: true,
  MediaControllerCustomX: -9999,
  MediaControllerCustomY: -9999,
  TransitionMotionBlurEnabled: true
};

function ensureSettingsDirectory() {
  if (!fs.existsSync(settingsDir)) {
    fs.mkdirSync(settingsDir, { recursive: true });
  }
}

function findOpenDockExe() {
  for (const p of possibleExePaths) {
    if (fs.existsSync(p)) return p;
  }
  return null;
}

function getPrimaryPinsPath() {
  for (const p of possiblePinsPaths) {
    if (fs.existsSync(p)) return p;
  }
  return possiblePinsPaths[0];
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1060,
    height: 740,
    minWidth: 920,
    minHeight: 620,
    frame: false,
    titleBarStyle: 'hidden',
    backgroundColor: '#0a0e17',
    icon: path.join(__dirname, 'renderer', 'icon.png'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  });

  mainWindow.loadFile(path.join(__dirname, 'renderer', 'index.html'));

  mainWindow.on('maximize', () => {
    mainWindow.webContents.send('window:maximize-change', true);
  });

  mainWindow.on('unmaximize', () => {
    mainWindow.webContents.send('window:maximize-change', false);
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

app.whenReady().then(() => {
  ensureSettingsDirectory();
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

// IPC: Window Controls
ipcMain.handle('window:minimize', () => {
  if (mainWindow) mainWindow.minimize();
});

ipcMain.handle('window:maximize', () => {
  if (!mainWindow) return false;
  if (mainWindow.isMaximized()) {
    mainWindow.unmaximize();
    return false;
  } else {
    mainWindow.maximize();
    return true;
  }
});

ipcMain.handle('window:close', () => {
  if (mainWindow) mainWindow.close();
});

ipcMain.handle('window:isMaximized', () => {
  return mainWindow ? mainWindow.isMaximized() : false;
});

// IPC: Settings
ipcMain.handle('settings:load', async () => {
  ensureSettingsDirectory();
  try {
    if (fs.existsSync(settingsPath)) {
      const content = fs.readFileSync(settingsPath, 'utf-8');
      const loaded = JSON.parse(content);
      return { ...defaultSettings, ...loaded };
    }
  } catch (err) {
    console.error('Failed to read settings file:', err);
  }
  return { ...defaultSettings };
});

ipcMain.handle('settings:save', async (_, newSettings) => {
  ensureSettingsDirectory();
  try {
    const finalSettings = { ...defaultSettings, ...newSettings };
    const json = JSON.stringify(finalSettings, null, 2);
    fs.writeFileSync(settingsPath, json, 'utf-8');


    return { success: true };
  } catch (err) {
    console.error('Failed to save settings file:', err);
    return { success: false, error: err.message };
  }
});

ipcMain.handle('settings:resetDefaults', async () => {
  ensureSettingsDirectory();
  try {
    const json = JSON.stringify(defaultSettings, null, 2);
    fs.writeFileSync(settingsPath, json, 'utf-8');


    return { success: true, settings: defaultSettings };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

ipcMain.handle('settings:export', async () => {
  if (!mainWindow) return { success: false };
  const { canceled, filePath } = await dialog.showSaveDialog(mainWindow, {
    title: 'Export OpenDock Settings',
    defaultPath: 'opendock_settings_backup.json',
    filters: [{ name: 'JSON Files', extensions: ['json'] }]
  });

  if (canceled || !filePath) return { success: false };

  try {
    let settingsToSave = defaultSettings;
    if (fs.existsSync(settingsPath)) {
      settingsToSave = JSON.parse(fs.readFileSync(settingsPath, 'utf-8'));
    }
    fs.writeFileSync(filePath, JSON.stringify(settingsToSave, null, 2), 'utf-8');
    return { success: true, filePath };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

ipcMain.handle('settings:import', async () => {
  if (!mainWindow) return { success: false };
  const { canceled, filePaths } = await dialog.showOpenDialog(mainWindow, {
    title: 'Import OpenDock Settings',
    filters: [{ name: 'JSON Files', extensions: ['json'] }],
    properties: ['openFile']
  });

  if (canceled || !filePaths || filePaths.length === 0) return { success: false };

  try {
    const raw = fs.readFileSync(filePaths[0], 'utf-8');
    const imported = JSON.parse(raw);
    const merged = { ...defaultSettings, ...imported };
    ensureSettingsDirectory();
    fs.writeFileSync(settingsPath, JSON.stringify(merged, null, 2), 'utf-8');
    return { success: true, settings: merged };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

ipcMain.handle('settings:openFolder', async () => {
  ensureSettingsDirectory();
  await shell.openPath(settingsDir);
  return true;
});

// IPC: Pinned Applications
ipcMain.handle('pinnedApps:load', async () => {
  const pinsPath = getPrimaryPinsPath();
  try {
    if (fs.existsSync(pinsPath)) {
      const data = JSON.parse(fs.readFileSync(pinsPath, 'utf-8'));
      return data.PinnedApps || [];
    }
  } catch (err) {
    console.error('Failed to load pinned apps:', err);
  }
  return [];
});

ipcMain.handle('pinnedApps:save', async (_, pinsList) => {
  const content = JSON.stringify({ PinnedApps: pinsList }, null, 2);
  ensureSettingsDirectory();
  try {
    for (const p of possiblePinsPaths) {
      try {
        const dir = path.dirname(p);
        if (fs.existsSync(dir)) {
          fs.writeFileSync(p, content, 'utf-8');
        }
      } catch { }
    }
    return { success: true };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

ipcMain.handle('pinnedApps:selectExecutable', async () => {
  if (!mainWindow) return null;
  const { canceled, filePaths } = await dialog.showOpenDialog(mainWindow, {
    title: 'Select Application Executable or Shortcut',
    filters: [
      { name: 'Executables and Shortcuts', extensions: ['exe', 'lnk'] },
      { name: 'All Files', extensions: ['*'] }
    ],
    properties: ['openFile']
  });

  if (canceled || !filePaths || filePaths.length === 0) return null;

  const fullPath = filePaths[0];
  const displayName = path.basename(fullPath, path.extname(fullPath));
  return { displayName, exePath: fullPath };
});

// IPC: Image Selector
ipcMain.handle('dialog:selectImage', async () => {
  if (!mainWindow) return null;
  const { canceled, filePaths } = await dialog.showOpenDialog(mainWindow, {
    title: 'Select Image or Icon',
    filters: [
      { name: 'Image Files', extensions: ['png', 'ico', 'jpg', 'jpeg', 'svg'] },
      { name: 'All Files', extensions: ['*'] }
    ],
    properties: ['openFile']
  });

  if (canceled || !filePaths || filePaths.length === 0) return null;
  return filePaths[0];
});

// IPC: OpenDock Process Control
function checkProcessRunning() {
  return new Promise((resolve) => {
    exec('tasklist /FI "IMAGENAME eq OpenDock.exe" /NH', (err, stdout) => {
      if (err || !stdout) return resolve(false);
      resolve(stdout.toLowerCase().includes('opendock.exe'));
    });
  });
}

ipcMain.handle('process:getStatus', async () => {
  const isRunning = await checkProcessRunning();
  const exePath = findOpenDockExe();
  return { isRunning, exePath };
});

ipcMain.handle('process:start', async () => {
  const exePath = findOpenDockExe();
  if (!exePath) return { success: false, error: 'OpenDock.exe not found. Please build the project.' };

  const isRunning = await checkProcessRunning();
  if (isRunning) return { success: true, message: 'OpenDock is already running.' };

  try {
    const child = spawn(exePath, [], {
      detached: true,
      stdio: 'ignore',
      cwd: path.dirname(exePath)
    });
    child.unref();
    return { success: true };
  } catch (err) {
    return { success: false, error: err.message };
  }
});

ipcMain.handle('process:stop', async () => {
  return new Promise((resolve) => {
    exec('taskkill /F /IM OpenDock.exe', (err) => {
      resolve({ success: !err });
    });
  });
});

ipcMain.handle('process:restart', async () => {
  return new Promise((resolve) => {
    exec('taskkill /F /IM OpenDock.exe', async () => {
      setTimeout(async () => {
        const exePath = findOpenDockExe();
        if (!exePath) return resolve({ success: false, error: 'OpenDock.exe not found' });
        try {
          const child = spawn(exePath, [], {
            detached: true,
            stdio: 'ignore',
            cwd: path.dirname(exePath)
          });
          child.unref();
          resolve({ success: true });
        } catch (err) {
          resolve({ success: false, error: err.message });
        }
      }, 400);
    });
  });
});

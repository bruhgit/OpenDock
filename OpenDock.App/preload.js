const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('api', {
  // Window controls for custom frameless titlebar
  window: {
    minimize: () => ipcRenderer.invoke('window:minimize'),
    maximize: () => ipcRenderer.invoke('window:maximize'),
    close: () => ipcRenderer.invoke('window:close'),
    isMaximized: () => ipcRenderer.invoke('window:isMaximized'),
    onMaximizeChange: (callback) => {
      ipcRenderer.on('window:maximize-change', (_, isMaximized) => callback(isMaximized));
    }
  },

  // OpenDock settings management
  settings: {
    load: () => ipcRenderer.invoke('settings:load'),
    save: (settings) => ipcRenderer.invoke('settings:save', settings),
    resetDefaults: () => ipcRenderer.invoke('settings:resetDefaults'),
    export: () => ipcRenderer.invoke('settings:export'),
    import: () => ipcRenderer.invoke('settings:import'),
    openFolder: () => ipcRenderer.invoke('settings:openFolder')
  },

  // Pinned dock applications
  pinnedApps: {
    load: () => ipcRenderer.invoke('pinnedApps:load'),
    save: (pins) => ipcRenderer.invoke('pinnedApps:save', pins),
    selectExecutable: () => ipcRenderer.invoke('pinnedApps:selectExecutable')
  },

  // OpenDock process management
  process: {
    getStatus: () => ipcRenderer.invoke('process:getStatus'),
    start: () => ipcRenderer.invoke('process:start'),
    restart: () => ipcRenderer.invoke('process:restart'),
    stop: () => ipcRenderer.invoke('process:stop')
  },

  // File pickers
  dialog: {
    selectImage: () => ipcRenderer.invoke('dialog:selectImage')
  }
});

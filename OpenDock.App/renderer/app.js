// ==========================================================================
// OpenDock Settings - Pure Native Client Logic (Zero AI Slop)
// ==========================================================================

let currentSettings = {};
let pinnedApps = [];
let saveTimeout = null;

// Helper: Color conversions
function argbToHex(argb) {
  const r = (argb >> 16) & 0xFF;
  const g = (argb >> 8) & 0xFF;
  const b = argb & 0xFF;
  return '#' + [r, g, b].map(x => x.toString(16).padStart(2, '0')).join('');
}

function hexAndAlphaToArgb(hex, alpha) {
  const cleanHex = hex.replace('#', '');
  const num = parseInt(cleanHex, 16) || 0;
  const r = (num >> 16) & 0xFF;
  const g = (num >> 8) & 0xFF;
  const b = num & 0xFF;
  // Return signed 32-bit integer matching System.Int32
  return (((alpha & 0xFF) << 24) | ((r & 0xFF) << 16) | ((g & 0xFF) << 8) | (b & 0xFF)) | 0;
}

// Minimal Toast notification
function showToast(message, type = 'success') {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast toast-${type}`;

  const iconSvg = type === 'success'
    ? `<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>`
    : `<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>`;

  toast.innerHTML = `${iconSvg}<span>${message}</span>`;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(6px)';
    toast.style.transition = 'all 150ms ease';
    setTimeout(() => toast.remove(), 150);
  }, 2000);
}

// ==========================================================================
// App Initialization
// ==========================================================================
document.addEventListener('DOMContentLoaded', async () => {
  initTitlebar();
  initSidebar();
  await loadAndBindSettings();
  await loadAndBindPinnedApps();
  initSystemTabActions();
  initProcessMonitoring();
});

// Titlebar caption buttons
function initTitlebar() {
  const btnMin = document.getElementById('btn-minimize');
  const btnMax = document.getElementById('btn-maximize');
  const btnClose = document.getElementById('btn-close');
  const iconMax = document.getElementById('icon-maximize');
  const iconRestore = document.getElementById('icon-restore');
  const btnQuickToggle = document.getElementById('btn-quick-toggle');

  if (btnMin) btnMin.addEventListener('click', () => window.api.window.minimize());
  if (btnMax) btnMax.addEventListener('click', () => window.api.window.maximize());
  if (btnClose) btnClose.addEventListener('click', () => window.api.window.close());

  window.api.window.onMaximizeChange((isMaximized) => {
    if (iconMax && iconRestore) {
      if (isMaximized) {
        iconMax.classList.add('hidden');
        iconRestore.classList.remove('hidden');
      } else {
        iconMax.classList.remove('hidden');
        iconRestore.classList.add('hidden');
      }
    }
  });

  if (btnQuickToggle) {
    btnQuickToggle.addEventListener('click', async () => {
      const status = await window.api.process.getStatus();
      if (status.isRunning) {
        showToast('Restarting OpenDock...');
        await window.api.process.restart();
      } else {
        showToast('Launching OpenDock...');
        await window.api.process.start();
      }
      setTimeout(updateProcessStatus, 600);
    });
  }
}

// Sidebar navigation
function initSidebar() {
  const navItems = document.querySelectorAll('.nav-item');
  navItems.forEach(item => {
    item.addEventListener('click', () => {
      const tabId = item.dataset.tab;
      navItems.forEach(i => i.classList.remove('active'));
      item.classList.add('active');

      document.querySelectorAll('.tab-pane').forEach(pane => {
        pane.classList.remove('active');
      });

      const activePane = document.getElementById(`tab-${tabId}`);
      if (activePane) activePane.classList.add('active');
    });
  });
}

// ==========================================================================
// Settings Data Binding
// ==========================================================================
async function loadAndBindSettings() {
  currentSettings = await window.api.settings.load();
  bindSettingsToUI();
}

function bindSettingsToUI() {
  // 1. General Tab - Dock Position Segmented Control
  const posSegmented = document.getElementById('position-segmented');
  if (posSegmented) {
    const currentPos = (currentSettings.DockPosition || 'Bottom').toLowerCase();
    posSegmented.querySelectorAll('.segment-btn').forEach(btn => {
      const btnPos = btn.dataset.pos.toLowerCase();
      if (btnPos === currentPos) {
        btn.classList.add('active');
      } else {
        btn.classList.remove('active');
      }

      btn.addEventListener('click', () => {
        posSegmented.querySelectorAll('.segment-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        currentSettings.DockPosition = btn.dataset.pos;
        saveImmediately('Position changed to ' + btn.dataset.pos);
      });
    });
  }

  // Toggles (General)
  bindToggle('setting-auto-hide', 'AutoHideEnabled', 'Auto-hide updated');
  bindToggle('setting-separate-apps', 'SeparateRunningApps', 'Running apps separator updated');
  bindToggle('setting-show-clock', 'ShowClock', 'Clock widget updated');
  bindToggle('setting-recycle-bin', 'ShowRecycleBin', 'Recycle bin updated');
  bindToggle('setting-media-controller', 'ShowMediaController', 'Media controller updated');
  bindToggle('setting-game-mode', 'GameModeEnabled', 'Game mode updated');

  // 2. Appearance Tab - Colors
  bindColorPair('input-dock-color-picker', 'input-dock-color-hex', 'DockColorArgb', () => currentSettings.DockBackgroundAlpha ?? 175);
  bindColorPair('input-menu-color-picker', 'input-menu-color-hex', 'MenuColorArgb', () => currentSettings.DockBackgroundAlpha ?? 175);
  bindColorPair('input-search-color-picker', 'input-search-color-hex', 'SearchColorArgb', () => 255);

  // Dock Alpha (Slider + Number)
  bindSliderAndNumber(
    'input-dock-alpha',
    'input-num-dock-alpha',
    'DockBackgroundAlpha',
    (v) => {
      const alpha = Math.round(v);
      currentSettings.DockBackgroundAlpha = alpha;
      const hex = argbToHex(currentSettings.DockColorArgb);
      currentSettings.DockColorArgb = hexAndAlphaToArgb(hex, alpha);
      const menuHex = argbToHex(currentSettings.MenuColorArgb);
      currentSettings.MenuColorArgb = hexAndAlphaToArgb(menuHex, alpha);
      return alpha;
    },
    0,
    255,
    30
  );

  // Start Menu Logo
  const logoInput = document.getElementById('input-logo-path');
  const logoPreview = document.getElementById('logo-preview');
  const btnBrowseLogo = document.getElementById('btn-browse-logo');
  const btnResetLogo = document.getElementById('btn-reset-logo');

  function updateLogoUI() {
    if (logoInput) logoInput.value = currentSettings.MenuLogoPath || 'Default Windows Logo';
    if (logoPreview) {
      if (currentSettings.MenuLogoPath) {
        logoPreview.innerHTML = `<img src="file://${currentSettings.MenuLogoPath.replace(/\\/g, '/')}" alt="Logo">`;
      } else {
        logoPreview.innerHTML = `<svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor"><path d="M0 3.449L9.75 2.1v9.451H0m10.949-9.602L24 0v11.4H10.949M0 12.6h9.75v9.451L0 20.699M10.949 12.6H24V24l-12.9-1.801"/></svg>`;
      }
    }
  }
  updateLogoUI();

  if (btnBrowseLogo) {
    btnBrowseLogo.addEventListener('click', async () => {
      const selected = await window.api.dialog.selectImage();
      if (selected) {
        currentSettings.MenuLogoPath = selected;
        updateLogoUI();
        saveImmediately('Custom start icon applied');
      }
    });
  }

  if (btnResetLogo) {
    btnResetLogo.addEventListener('click', () => {
      currentSettings.MenuLogoPath = '';
      updateLogoUI();
      saveImmediately('Default start icon restored');
    });
  }

  // 3. Size & Magnification Tab
  bindSliderAndNumber('input-dock-length', 'input-num-dock-length', 'DockLength', Math.round, 320, 3840, 30);
  bindSliderAndNumber('input-icon-size', 'input-num-icon-size', 'DockIconSize', Math.round, 16, 96, 30);
  bindToggle('setting-magnification-enabled', 'MagnificationEnabled', 'Magnification updated');
  bindSliderAndNumber('input-magnified-size', 'input-num-magnified-size', 'MagnifiedIconSize', Math.round, 24, 128, 30);
  bindSliderAndNumber('input-magnification-range', 'input-num-magnification-range', 'MagnificationRange', Math.round, 1, 5, 30);

  // 4. Transitions Tab
  const minEffectSegmented = document.getElementById('minimize-effect-segmented');
  if (minEffectSegmented) {
    const currentEffect = (currentSettings.MinimizeEffect || 'Genie').toLowerCase();
    minEffectSegmented.querySelectorAll('.segment-btn').forEach(btn => {
      const btnEffect = btn.dataset.effect.toLowerCase();
      if (btnEffect === currentEffect) {
        btn.classList.add('active');
      } else {
        btn.classList.remove('active');
      }

      btn.addEventListener('click', () => {
        minEffectSegmented.querySelectorAll('.segment-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        currentSettings.MinimizeEffect = btn.dataset.effect;
        saveImmediately('Minimize effect set to ' + btn.dataset.effect);
      });
    });
  }

  bindToggle('setting-motion-blur', 'TransitionMotionBlurEnabled', 'Motion blur updated');
  bindToggle('setting-show-stars', 'TransitionShowStars', 'Star field updated');
  bindSliderAndNumber('input-stars-count', 'input-num-stars-count', 'TransitionStarsCount', Math.round, 5, 100, 30);
  bindSliderAndNumber('input-deceleration', 'input-num-deceleration', 'TransitionDeceleration', (v) => parseFloat(v.toFixed(2)), 0.05, 0.50, 30);
  bindSliderAndNumber('input-switch-delay', 'input-num-switch-delay', 'TransitionSwitchDelay', Math.round, 0, 400, 30);
}

// Toggle Helper
function bindToggle(elementId, settingKey, toastMsg) {
  const el = document.getElementById(elementId);
  if (!el) return;
  el.checked = !!currentSettings[settingKey];
  el.addEventListener('change', () => {
    currentSettings[settingKey] = el.checked;
    saveImmediately(toastMsg);
  });
}

// Paired Slider & Number Input Helper
function bindSliderAndNumber(sliderId, numId, settingKey, transform, min, max, debounceMs = 30) {
  const slider = document.getElementById(sliderId);
  const numInput = document.getElementById(numId);
  if (!slider || !numInput) return;

  let val = currentSettings[settingKey];
  if (val !== undefined) {
    slider.value = val;
    numInput.value = val;
  }

  function applyValue(newVal) {
    let parsed = parseFloat(newVal);
    if (isNaN(parsed)) return;
    if (min !== undefined) parsed = Math.max(min, parsed);
    if (max !== undefined) parsed = Math.min(max, parsed);

    slider.value = parsed;
    numInput.value = parsed;
    currentSettings[settingKey] = (transform ? transform(parsed) : parsed);
    debouncedSave(debounceMs);
  }

  slider.addEventListener('input', (e) => applyValue(e.target.value));
  numInput.addEventListener('change', (e) => applyValue(e.target.value));
}

// Color Pair Helper
function bindColorPair(pickerId, hexId, settingKey, alphaGetter) {
  const picker = document.getElementById(pickerId);
  const hexInput = document.getElementById(hexId);
  if (!picker || !hexInput) return;

  const initialHex = argbToHex(currentSettings[settingKey] || 0);
  picker.value = initialHex;
  hexInput.value = initialHex.toUpperCase();

  picker.addEventListener('input', () => {
    hexInput.value = picker.value.toUpperCase();
    const alpha = alphaGetter ? alphaGetter() : 175;
    currentSettings[settingKey] = hexAndAlphaToArgb(picker.value, alpha);
    debouncedSave(30);
  });

  hexInput.addEventListener('change', () => {
    let val = hexInput.value.trim();
    if (!val.startsWith('#')) val = '#' + val;
    if (/^#[0-9A-Fa-f]{6}$/.test(val)) {
      picker.value = val;
      hexInput.value = val.toUpperCase();
      const alpha = alphaGetter ? alphaGetter() : 175;
      currentSettings[settingKey] = hexAndAlphaToArgb(val, alpha);
      debouncedSave(30);
    } else {
      hexInput.value = picker.value.toUpperCase();
    }
  });
}

// Immediate Save
function saveImmediately(toastMsg) {
  clearTimeout(saveTimeout);
  window.api.settings.save(currentSettings).then(res => {
    if (res.success && toastMsg) {
      showToast(toastMsg);
    } else if (!res.success) {
      showToast('Save failed: ' + (res.error || 'Unknown error'), 'error');
    }
  });
}

// Debounced Autosave (30ms for buttery-smooth slider tracking)
function debouncedSave(delayMs = 30) {
  clearTimeout(saveTimeout);
  saveTimeout = setTimeout(async () => {
    const res = await window.api.settings.save(currentSettings);
    if (!res.success) {
      showToast('Save failed: ' + (res.error || 'Unknown error'), 'error');
    }
  }, delayMs);
}

// ==========================================================================
// Pinned Applications Management
// ==========================================================================
async function loadAndBindPinnedApps() {
  pinnedApps = await window.api.pinnedApps.load();
  renderPinnedAppsList();

  const btnAddPin = document.getElementById('btn-add-pin');
  if (btnAddPin) {
    btnAddPin.addEventListener('click', async () => {
      const selected = await window.api.pinnedApps.selectExecutable();
      if (selected) {
        const exists = pinnedApps.some(p => (p.ExePath || '').toLowerCase() === selected.exePath.toLowerCase());
        if (exists) {
          showToast('Application is already pinned.', 'error');
          return;
        }

        pinnedApps.push({
          DisplayName: selected.displayName,
          ExePath: selected.exePath
        });

        await window.api.pinnedApps.save(pinnedApps);
        renderPinnedAppsList();
        showToast(`Pinned ${selected.displayName}`);
      }
    });
  }
}

function renderPinnedAppsList() {
  const container = document.getElementById('pins-list-container');
  if (!container) return;

  if (!pinnedApps || pinnedApps.length === 0) {
    container.innerHTML = '<div class="empty-state">No applications currently pinned to OpenDock. Click "Add Application..." to pin one.</div>';
    return;
  }

  container.innerHTML = '';
  pinnedApps.forEach((pin, idx) => {
    const row = document.createElement('div');
    row.className = 'pin-row';

    const displayName = pin.DisplayName || 'Application';
    const exePath = pin.ExePath || '';

    row.innerHTML = `
      <div class="pin-info">
        <div class="pin-icon-box">
          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="2" y="3" width="20" height="14" rx="2"></rect>
            <line x1="8" y1="21" x2="16" y2="21"></line>
            <line x1="12" y1="17" x2="12" y2="21"></line>
          </svg>
        </div>
        <div class="pin-meta">
          <span class="pin-title">${escapeHtml(displayName)}</span>
          <span class="pin-path" title="${escapeHtml(exePath)}">${escapeHtml(exePath)}</span>
        </div>
      </div>
      <button type="button" class="btn-delete-pin" title="Unpin application" aria-label="Unpin application">
        <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <polyline points="3 6 5 6 21 6"></polyline>
          <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
        </svg>
      </button>
    `;

    row.querySelector('.btn-delete-pin')?.addEventListener('click', async () => {
      const removed = pinnedApps.splice(idx, 1);
      await window.api.pinnedApps.save(pinnedApps);
      renderPinnedAppsList();
      showToast(`Unpinned ${removed[0]?.DisplayName || 'application'}`);
    });

    container.appendChild(row);
  });
}

function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// ==========================================================================
// System Actions & Process Supervisor
// ==========================================================================
function initSystemTabActions() {
  const btnStart = document.getElementById('btn-start-dock');
  const btnRestart = document.getElementById('btn-restart-dock');
  const btnStop = document.getElementById('btn-stop-dock');
  const btnExport = document.getElementById('btn-export-settings');
  const btnImport = document.getElementById('btn-import-settings');
  const btnOpenFolder = document.getElementById('btn-open-folder');
  const btnResetDefaults = document.getElementById('btn-reset-defaults');

  if (btnStart) {
    btnStart.addEventListener('click', async () => {
      const res = await window.api.process.start();
      if (res.success) {
        showToast('OpenDock launched');
      } else {
        showToast(res.error || 'Failed to start', 'error');
      }
      setTimeout(updateProcessStatus, 600);
    });
  }

  if (btnRestart) {
    btnRestart.addEventListener('click', async () => {
      showToast('Restarting OpenDock...');
      const res = await window.api.process.restart();
      if (res.success) {
        showToast('OpenDock restarted successfully');
      } else {
        showToast(res.error || 'Restart failed', 'error');
      }
      setTimeout(updateProcessStatus, 600);
    });
  }

  if (btnStop) {
    btnStop.addEventListener('click', async () => {
      await window.api.process.stop();
      showToast('OpenDock terminated');
      setTimeout(updateProcessStatus, 500);
    });
  }

  if (btnExport) {
    btnExport.addEventListener('click', async () => {
      const res = await window.api.settings.export();
      if (res.success) {
        showToast(`Settings exported to ${res.filePath}`);
      }
    });
  }

  if (btnImport) {
    btnImport.addEventListener('click', async () => {
      const res = await window.api.settings.import();
      if (res.success && res.settings) {
        currentSettings = res.settings;
        bindSettingsToUI();
        showToast('Settings imported successfully');
      }
    });
  }

  if (btnOpenFolder) {
    btnOpenFolder.addEventListener('click', () => {
      window.api.settings.openFolder();
    });
  }

  if (btnResetDefaults) {
    btnResetDefaults.addEventListener('click', async () => {
      const res = await window.api.settings.resetDefaults();
      if (res.success && res.settings) {
        currentSettings = res.settings;
        bindSettingsToUI();
        showToast('Settings restored to factory defaults');
      }
    });
  }
}

// Process status polling
function initProcessMonitoring() {
  updateProcessStatus();
  setInterval(updateProcessStatus, 2000);
}

async function updateProcessStatus() {
  const status = await window.api.process.getStatus();
  const pill = document.getElementById('dock-status-pill');
  const text = document.getElementById('dock-status-text');
  const quickBtn = document.getElementById('btn-quick-toggle');
  const quickBtnText = document.getElementById('btn-quick-toggle-text');
  const sysBadge = document.getElementById('sys-status-badge');
  const sysPath = document.getElementById('sys-exe-path');

  if (status.isRunning) {
    if (pill) pill.className = 'status-pill status-running';
    if (text) text.textContent = 'Dock Running';
    if (quickBtnText) quickBtnText.textContent = 'Restart';
    if (sysBadge) {
      sysBadge.className = 'badge badge-success';
      sysBadge.textContent = 'Active / Running';
    }
  } else {
    if (pill) pill.className = 'status-pill status-stopped';
    if (text) text.textContent = 'Dock Stopped';
    if (quickBtnText) quickBtnText.textContent = 'Launch';
    if (sysBadge) {
      sysBadge.className = 'badge badge-danger';
      sysBadge.textContent = 'Inactive / Stopped';
    }
  }

  if (sysPath) {
    sysPath.textContent = status.exePath || 'OpenDock.exe not found (build required)';
  }
}

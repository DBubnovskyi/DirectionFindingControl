const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { SerialPort } = require('serialport');

// Live reload в режимі розробки
try {
  require('electron-reload')(__dirname, {
    electron: path.join(__dirname, 'node_modules', '.bin', 'electron'),
    hardResetMethod: 'exit'
  });
} catch (err) {
  // electron-reload не доступній в production білді
}

// IPC handlers для серійних портів
ipcMain.handle('serial:list-ports', async () => {
  try {
    const ports = await SerialPort.list();
    return ports.map(port => ({
      value: port.path,
      label: `${port.path}${port.friendlyName ? ` - ${port.friendlyName}` : ''}`
    }));
  } catch (error) {
    console.error('Error listing serial ports:', error);
    return [];
  }
});

function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    autoHideMenuBar: true,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    }
  });

  mainWindow.loadFile('dist/index.html');

  // Open DevTools in development
  // mainWindow.webContents.openDevTools();
}

app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

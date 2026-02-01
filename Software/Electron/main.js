const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { SerialPort } = require('serialport');

let mainWindow = null;
let serialPort = null;

// Live reload в режимі розробки
try {
  require('electron-reload')(__dirname, {
    electron: path.join(__dirname, 'node_modules', '.bin', 'electron'),
    hardResetMethod: 'exit'
  });
} catch (err) {
  // electron-reload не доступній в production білді
}

function createWindow() {
  mainWindow = new BrowserWindow({
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
}

app.whenReady().then(() => {
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

  // Відкриття серійного порту
  ipcMain.handle('serial:open', async (event, portPath, baudRate) => {
    try {
      if (serialPort && serialPort.isOpen) {
        await new Promise((resolve) => serialPort.close(resolve));
      }

      serialPort = new SerialPort({
        path: portPath,
        baudRate: parseInt(baudRate)
      });

      serialPort.on('data', (data) => {
        if (mainWindow) {
          mainWindow.webContents.send('serial:data', data.toString());
        }
      });

      serialPort.on('error', (err) => {
        console.error('Serial port error:', err);
      });

      return { success: true };
    } catch (error) {
      console.error('Error opening serial port:', error);
      return { success: false, error: error.message };
    }
  });

  // Закриття серійного порту
  ipcMain.handle('serial:close', async () => {
    try {
      if (serialPort && serialPort.isOpen) {
        await new Promise((resolve) => serialPort.close(resolve));
        serialPort = null;
      }
      return { success: true };
    } catch (error) {
      console.error('Error closing serial port:', error);
      return { success: false, error: error.message };
    }
  });

  // Запис в серійний порт
  ipcMain.handle('serial:write', async (event, data) => {
    try {
      if (!serialPort || !serialPort.isOpen) {
        return { success: false, error: 'Serial port is not open' };
      }

      await new Promise((resolve, reject) => {
        serialPort.write(data, (err) => {
          if (err) reject(err);
          else resolve();
        });
      });

      return { success: true };
    } catch (error) {
      console.error('Error writing to serial port:', error);
      return { success: false, error: error.message };
    }
  });

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

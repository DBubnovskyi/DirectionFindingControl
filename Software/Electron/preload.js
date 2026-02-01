const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  listSerialPorts: () => ipcRenderer.invoke('serial:list-ports'),
  serialOpen: (path, baudRate) => ipcRenderer.invoke('serial:open', path, baudRate),
  serialClose: () => ipcRenderer.invoke('serial:close'),
  serialWrite: (data) => ipcRenderer.invoke('serial:write', data),
  onSerialData: (callback) => ipcRenderer.on('serial:data', (event, data) => callback(data)),
  removeSerialDataListener: () => ipcRenderer.removeAllListeners('serial:data')
});

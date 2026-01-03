const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  listSerialPorts: () => ipcRenderer.invoke('serial:list-ports')
});

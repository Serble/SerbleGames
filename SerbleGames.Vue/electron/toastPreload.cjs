'use strict';

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('toastBridge', {
  /** Register a callback that receives achievement data and renders a toast. */
  onAddToast: (callback) => {
    const handler = (_, data) => callback(data);
    ipcRenderer.on('add-toast', handler);
    return () => ipcRenderer.removeListener('add-toast', handler);
  },

  /** Called by the toast page when it has at least one visible toast. */
  show: () => ipcRenderer.send('toast-show'),

  /** Called by the toast page when its queue empties. */
  hide: () => ipcRenderer.send('toast-hide'),
});

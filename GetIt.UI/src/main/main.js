'use strict'

import { app, BrowserWindow } from 'electron'

const isDevelopment = !app.isPackaged

// global reference to mainWindow (necessary to prevent window from being garbage collected)
let mainWindow

function createMainWindow() {
  const window = new BrowserWindow({webPreferences: {nodeIntegration: true}})
  if (process.env.ELECTRON_WINDOW_SIZE)
  {
    let [width, height] = process.env.ELECTRON_WINDOW_SIZE.split("x")
    window.setSize(parseInt(width), parseInt(height));
  }
  if (process.env.ELECTRON_START_MAXIMIZED)
  {
    window.maximize();
  }

  if (isDevelopment && !process.env.GET_IT_TEST) {
    window.webContents.openDevTools()
  }
  if (isDevelopment) {
    window.loadURL(`http://localhost:${process.env.ELECTRON_WEBPACK_WDS_PORT}`)
  }
  else {
    window.loadFile('index.html')
  }

  window.setMenu(null)

  window.on('closed', () => {
    mainWindow = null
  })

  window.webContents.on('devtools-opened', () => {
    window.focus()
    setImmediate(() => {
      window.focus()
    })
  })

  return window
}

// quit application when all windows are closed
app.on('window-all-closed', () => {
  // on macOS it is common for applications to stay open until the user explicitly quits
  if (process.platform !== 'darwin') {
    app.quit()
  }
})

app.on('activate', () => {
  // on macOS it is common to re-create a window even after all windows have been closed
  if (mainWindow === null) {
    mainWindow = createMainWindow()
  }
})

// create main BrowserWindow when electron is ready
app.on('ready', () => {
  mainWindow = createMainWindow()
})

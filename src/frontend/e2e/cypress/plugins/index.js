/// <reference types="cypress" />
// ***********************************************************
// This example plugins/index.js can be used to load plugins
//
// You can change the location of this file or turn off loading
// the plugins file with the 'pluginsFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/plugins-guide
// ***********************************************************

// This function is called when a project is opened or re-opened (e.g. due to
// the project's config changing)

/**
 * @type {Cypress.PluginConfig}
 */
// eslint-disable-next-line no-unused-vars
const readXlsxFile = require('read-excel-file/node')
const { rmdir } = require('fs')

module.exports = (on, config) => {
  
    // register utility tasks to read and parse Excel files
    on('task', {
        readExcelFile(filename){
            console.log('reading Excel file %s', filename)
            console.log('from cwd %s', process.cwd())

            return readXlsxFile(filename)
        },
        
        deleteFolder(folderName) {
            console.log('deleting folder %s', folderName)

            return new Promise((resolve, reject) => {
                rmdir(folderName, { maxRetries: 10, recursive: true }, (err) => {
                    if (err) {
                        console.error(err)

                        return reject(err)
                    }

                    resolve(null)
                })
            })
        },
    })
}
import "core-js/modules/es.regexp.exec.js";
import "core-js/modules/es.string.split.js";
// addons, panels and events get unique names using a prefix
export var PARAM_KEY = 'test';
export var ADDON_ID = 'storybookjs/test';
export var PANEL_ID = "".concat(ADDON_ID, "/panel");
export var ADD_TESTS = "".concat(ADDON_ID, "/add_tests");
export function defineJestParameter(parameters) {
  var jest = parameters.jest,
      filePath = parameters.fileName;

  if (typeof jest === 'string') {
    return [jest];
  }

  if (jest && Array.isArray(jest)) {
    return jest;
  }

  if (jest === undefined && typeof filePath === 'string') {
    var fileName = filePath.split('/').pop().split('.')[0];
    return [fileName];
  }

  return null;
}
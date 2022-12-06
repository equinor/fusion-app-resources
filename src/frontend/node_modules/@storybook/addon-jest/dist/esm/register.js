import * as React from 'react';
import addons from '@storybook/addons';
import { ADDON_ID, PANEL_ID, PARAM_KEY } from './shared';
import Panel from './components/Panel';
addons.register(ADDON_ID, function (api) {
  addons.addPanel(PANEL_ID, {
    title: 'Tests',
    render: function render(_ref) {
      var active = _ref.active,
          key = _ref.key;
      return /*#__PURE__*/React.createElement(Panel, {
        key: key,
        api: api,
        active: active
      });
    },
    paramKey: PARAM_KEY
  });
});
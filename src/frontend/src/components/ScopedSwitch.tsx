import * as React from 'react';
import { SwitchProps, useRouteMatch, Switch, useLocation } from 'react-router-dom';
import { createLocation } from 'history';

const ScopedSwitch: React.FC<SwitchProps> = props => {
    const match = useRouteMatch();
    const location = useLocation();
    const scopedLocation = createLocation(location.pathname.replace(match.url, ''));

    return <Switch {...props} location={scopedLocation} />;
};

export default ScopedSwitch;
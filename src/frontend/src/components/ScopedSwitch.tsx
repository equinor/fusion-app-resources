
import { SwitchProps, useRouteMatch, Switch, useLocation } from 'react-router-dom';
import { createLocation } from 'history';
import { FC } from 'react';

const ScopedSwitch: FC<SwitchProps> = props => {
    const match = useRouteMatch();
    const location = useLocation();
    const scopedLocation = createLocation(location.pathname.replace(match.url, ''));

    return <Switch {...props} location={scopedLocation} />;
};

export default ScopedSwitch;
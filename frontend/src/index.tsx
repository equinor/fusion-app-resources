import * as React from 'react';
import { registerApp } from '@equinor/fusion';

const App: React.FC = () => {
    
    return (
        <div>
            Resources
        </div>
    );
};

registerApp('resources', {
    AppComponent: App,
});

if (module.hot) {
    module.hot.accept();
}

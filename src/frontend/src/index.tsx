import * as React from 'react';
import {
    registerApp,
    ContextTypes,
    Context,
    useFusionContext,
    useCurrentContext,
} from '@equinor/fusion';
import { Switch, Route } from 'react-router-dom';
import LandingPage from './pages/LandingPage';
import ProjectPage from './pages/ProjectPage';
import ApiClient from './api/ApiClient';
import AppContext from './appContext';
import { appReducer, createInitialState } from './reducers/appReducer';
import useCollectionReducer from './hooks/useCollectionReducer';

const App: React.FC = () => {
    const fusionContext = useFusionContext();
    const apiClient = React.useMemo(
        () => new ApiClient(fusionContext.http.client, 'https://resources-api.ci.fusion-dev.net'),
        [fusionContext.http.client]
    );

    React.useEffect(() => {
        fusionContext.auth.container.registerAppAsync('5a842df8-3238-415d-b168-9f16a6a6031b', [
            'https://resources-api.ci.fusion-dev.net',
        ]);
    }, []);

    const currentContext = useCurrentContext();
    const [appState, dispatchAppAction] = useCollectionReducer(
        currentContext?.id || 'app',
        appReducer,
        createInitialState()
    );

    return (
        <AppContext.Provider value={{ apiClient, appState, dispatchAppAction }}>
            <Switch>
                <Route path="/" exact component={LandingPage} />
                <Route path="/:projectId" component={ProjectPage} />
            </Switch>
        </AppContext.Provider>
    );
};

registerApp('resources', {
    AppComponent: App,
    name: 'Resources',
    context: {
        types: [ContextTypes.OrgChart],
        buildUrl: (context: Context | null) => {
            return context?.id || '';
        },
        getContextFromUrl: (url: string) => {
            return url.split('/')[0];
        },
    },
} as any);

if (module.hot) {
    module.hot.accept();
}

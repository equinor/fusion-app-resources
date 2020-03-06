import * as React from 'react';
import { registerApp, ContextTypes, Context, useFusionContext } from '@equinor/fusion';
import JSON from '@equinor/fusion/lib/utils/JSON';
import { Switch, Route } from 'react-router-dom';
import LandingPage from './pages/LandingPage';
import ProjectPage from './pages/ProjectPage';
import ApiClient from './api/ApiClient';
import AppContext from './appContext';
import { appReducer } from './appReducer';

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

    const SESSION_STATE_KEY = 'FUSION_CONTRACT_APP_STATE';
    const sessionState = sessionStorage.getItem(SESSION_STATE_KEY);
    const [appState, dispatchAppAction] = React.useReducer(appReducer, sessionState ? JSON.parse(sessionState) : {
        contracts: { isFetching: false, data: [], error: null },
        positions: { isFetching: false, data: [], error: null },
    });

    sessionStorage.setItem(SESSION_STATE_KEY, JSON.stringify(appState));

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

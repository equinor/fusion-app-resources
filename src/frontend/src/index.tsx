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
import ServiceNowApiClient from './api/ServiceNowApiClient';

const RESOURCE_BASE_URL = 'https://resources-api.ci.fusion-dev.net';

// "https://pro-f-utility-CI.azurewebsites.net"
// "https://pro-f-common-CI.azurewebsites.net"
const FUNCTION_BASE_URL = 'https://pro-f-common-CI.azurewebsites.net';

const App: React.FC = () => {
    const fusionContext = useFusionContext();
    const apiClient = React.useMemo(
        () => new ApiClient(fusionContext.http.client, RESOURCE_BASE_URL),
        [fusionContext.http.client]
    );

    const serviceNowApiClient = React.useMemo(
        () => new ServiceNowApiClient(fusionContext.http.client, FUNCTION_BASE_URL),
        [fusionContext.http.client]
    );

    React.useEffect(() => {
        fusionContext.auth.container.registerAppAsync('5a842df8-3238-415d-b168-9f16a6a6031b', [
            RESOURCE_BASE_URL,
        ]);

        fusionContext.auth.container.registerAppAsync('5a842df8-3238-415d-b168-9f16a6a6031b', [
            FUNCTION_BASE_URL,
        ]);
    }, []);

    const currentContext = useCurrentContext();
    const [appState, dispatchAppAction] = useCollectionReducer(
        currentContext?.id || 'app',
        appReducer,
        createInitialState()
    );

    const prevContext = React.useRef<Context | null>(null);
    React.useEffect(() => {
        if (!prevContext.current) {
            prevContext.current = currentContext;
            return;
        }

        if (!currentContext || currentContext.id !== prevContext.current.id) {
            dispatchAppAction({ verb: 'reset', collection: 'contracts' });
            dispatchAppAction({ verb: 'reset', collection: 'positions' });
            dispatchAppAction({ verb: 'reset', collection: 'basePositions' });
        }
    }, [currentContext]);

    return (
        <AppContext.Provider
            value={{ apiClient, serviceNowApiClient, appState, dispatchAppAction }}
        >
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
});

if (module.hot) {
    module.hot.accept();
}

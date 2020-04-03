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
import { getResourceApiBaseUrl, getFunctionsBaseUrl } from './api/env';

const App: React.FC = () => {
    const fusionContext = useFusionContext();

    const resourceBaseUrl = React.useMemo(() => getResourceApiBaseUrl(fusionContext.environment), [
        fusionContext.environment,
    ]);
    const functionsBaseUrl = React.useMemo(() => getFunctionsBaseUrl(fusionContext.environment), [
        fusionContext.environment,
    ]);

    const apiClient = React.useMemo(
        () => new ApiClient(fusionContext.http.client, resourceBaseUrl),
        [fusionContext.http.client]
    );

    const serviceNowApiClient = React.useMemo(
        () => new ServiceNowApiClient(fusionContext.http.client, functionsBaseUrl),
        [fusionContext.http.client]
    );

    React.useEffect(() => {
        fusionContext.auth.container.registerAppAsync('5a842df8-3238-415d-b168-9f16a6a6031b', [
            resourceBaseUrl,
        ]);

        fusionContext.auth.container.registerAppAsync('5a842df8-3238-415d-b168-9f16a6a6031b', [
            functionsBaseUrl,
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
        buildUrl: (context: Context | null, url: string) => {
            console.log(context, url)
            return context && url.includes(context.id) ? url : context?.id || '';
        },
        getContextFromUrl: (url: string) => {
            return url.split('/')[0];
        },
    },
});

if (module.hot) {
    module.hot.accept();
}

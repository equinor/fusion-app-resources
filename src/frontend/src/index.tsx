
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
import { getResourceApiBaseUrl, getFunctionsBaseUrl, getFusionAppId } from './api/env';
import HelpPage from './pages/HelpPage';
import { FC, useMemo, useEffect, useRef } from 'react';

const App: FC = () => {
    const fusionContext = useFusionContext();

    const resourceBaseUrl = useMemo(() => getResourceApiBaseUrl(fusionContext.environment), [
        fusionContext.environment,
    ]);
    const functionsBaseUrl = useMemo(() => getFunctionsBaseUrl(fusionContext.environment), [
        fusionContext.environment,
    ]);

    const apiClient = useMemo(
        () => new ApiClient(fusionContext.http.client, resourceBaseUrl),
        [fusionContext.http.client]
    );

    const serviceNowApiClient = useMemo(
        () => new ServiceNowApiClient(fusionContext.http.client, functionsBaseUrl),
        [fusionContext.http.client]
    );

    useEffect(() => {
        fusionContext.auth.container.registerAppAsync(getFusionAppId(), [resourceBaseUrl]);

        fusionContext.auth.container.registerAppAsync(getFusionAppId(), [functionsBaseUrl]);
    }, []);

    const currentContext = useCurrentContext();
    const [appState, dispatchAppAction] = useCollectionReducer(
        currentContext?.id || 'app',
        appReducer,
        createInitialState()
    );

    const prevContext = useRef<Context | null>(null);
    useEffect(() => {
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
                <Route path="/help" component={HelpPage} exact />

                <Route path="/:projectId" component={ProjectPage} />
            </Switch>
        </AppContext.Provider>
    );
};

registerApp('resources', {
    AppComponent: App,
    name: 'External personnel',
    context: {
        types: [ContextTypes.OrgChart],
        buildUrl: (context: Context | null, url: string) => {
            return (context && url.includes(context.id)) || url.includes('/help')
                ? url
                : context?.id || '';
        },
        getContextFromUrl: (url: string) => url.split('/').filter(Boolean)[0],
    },
});

if (module.hot) {
    module.hot.accept();
}

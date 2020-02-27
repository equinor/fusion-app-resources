import * as React from 'react';
import { registerApp, ContextTypes, Context } from '@equinor/fusion';
import { Switch, Route } from 'react-router-dom';
import LandingPage from './pages/LandingPage';
// import ContractsOverviewPage from './pages/ContractsOverviewPage';
// import ContractDetailsPage from './pages/ContractDetailsPage';
import ProjectPage from './pages/ProjectPage';

const App: React.FC = () => {
    return (
        <Switch>
            <Route path="/" exact component={LandingPage} />
            <Route path="/:projectId" component={ProjectPage} />
            {/* <Route path="/:projectId/:contractId" exact component={ContractDetailsPage} /> */}
        </Switch>
    );
};

registerApp('resources', {
    AppComponent: App,
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

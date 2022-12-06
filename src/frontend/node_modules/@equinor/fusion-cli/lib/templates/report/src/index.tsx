import { FC, useMemo } from 'react';
import {
    registerApp,
    useFusionContext,
    ContextTypes,
    Context,
    useCurrentContext,
} from '@equinor/fusion';
import { ErrorBoundary, PowerBIReport, IBasicFilter } from '@equinor/fusion-components';
import LandingPage from './LandingPage';

const App: FC = () => {
    const currentContext = useCurrentContext();
    const fusionContext = useFusionContext();

    const reportId = fusionContext.environment.env === 'FPRD' ? '{REPORTIDPROD}' : '{REPORTIDTEST}';

    const filter = useMemo((): IBasicFilter => {
        return {
            $schema: 'http://powerbi.com/product/schema#basic',
            target: {
                table: 'Dim_MasterProject',
                column: 'Project',
            },
            filterType: 1,
            operator: 'In',
            values: [currentContext?.title || 'No context. Show empty report'],
        };
    }, [currentContext?.id]);

    return (
        <ErrorBoundary>
            {!currentContext?.id && <LandingPage />}
            {currentContext?.id && <PowerBIReport reportId={reportId} filters={[filter]} />}
        </ErrorBoundary>
    );
};

registerApp('{appKey}', {
    AppComponent: App,
    context: {
        types: [ContextTypes.ProjectMaster],
        buildUrl: (context: Context | null, url: string) => {
            if (!context) return '';
            return `/${context.id}`;
        },
        getContextFromUrl: (url: string) => {
            const contextId = url.replace('/', '');
            return contextId.length > 10 ? contextId : '';
        },
    },
});

if (module.hot) {
    module.hot.accept();
}

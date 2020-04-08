import * as React from 'react';
import * as styles from './styles.less';
import { Spinner, MarkdownViewer, ErrorMessage } from '@equinor/fusion-components';

import GithubApiClient from '../../api/GithubApiClient';

const HelpPage: React.FC = () => {
    const [isFetchingHelpPage, setIsFetchingHelpPage] = React.useState<boolean>(false);
    const [helpPageError, setHelpPageError] = React.useState<Error | null>(null);
    const [helpPageMarkdown, setHelpPageMarkdown] = React.useState<string>('');
    const githubApiClient = React.useMemo(
        () =>
            new GithubApiClient(
                'https://raw.githubusercontent.com/equinor/fusion-app-resources/50a02e82a9115610a34510053c76cfd86a49b4e2'
            ),
        []
    );

    const fetchHelpPageAsync = React.useCallback(async () => {
        setIsFetchingHelpPage(true);
        setHelpPageError(null);
        try {
            const response = await githubApiClient.getContractManagementAsync();
            setHelpPageMarkdown(response);
        } catch (e) {
            setHelpPageError(e);
        } finally {
            setIsFetchingHelpPage(false);
        }
    }, [githubApiClient]);

    React.useEffect(() => {
        fetchHelpPageAsync();
    }, [githubApiClient]);

    if (helpPageError) {
        return <ErrorMessage hasError title="Unable to fetch help page" />;
    }

    if (isFetchingHelpPage) {
        return <Spinner centered />;
    }

    return (
        <div className={styles.helpContainer}>
            <MarkdownViewer markdown={helpPageMarkdown} />
        </div>
    );
};
export default HelpPage;

import * as React from 'react';
import * as styles from './styles.less';
import { Spinner, MarkdownViewer, ErrorMessage, Tabs, Tab } from '@equinor/fusion-components';

import GithubApiClient from '../../api/GithubApiClient';

type TabKeys = 'resource-management' | 'role-delegation' | string;

const HelpPage: React.FC = () => {
    const [isFetchingHelpPage, setIsFetchingHelpPage] = React.useState<boolean>(false);
    const [helpPageError, setHelpPageError] = React.useState<Error | null>(null);
    const [helpPageMarkdown, setHelpPageMarkdown] = React.useState<string>('');
    const [selectedTab, setSelectedTab] = React.useState<TabKeys>('resource-management');
    const githubApiClient = React.useMemo(
        () =>
            new GithubApiClient(
                'https://raw.githubusercontent.com/equinor/fusion-app-resources/master'
            ),
        []
    );

    const fetchHelpPageAsync = React.useCallback(async () => {
        setIsFetchingHelpPage(true);
        setHelpPageError(null);
        try {
            const response =
                selectedTab === 'role-delegation'
                    ? await githubApiClient.getRoleDelegationAsync()
                    : await githubApiClient.getContractManagementAsync();
            setHelpPageMarkdown(response);
        } catch (e) {
            setHelpPageError(e);
        } finally {
            setIsFetchingHelpPage(false);
        }
    }, [githubApiClient, selectedTab]);

    React.useEffect(() => {
        if (githubApiClient && selectedTab) {
            fetchHelpPageAsync();
        }
    }, [githubApiClient, selectedTab]);

    if (helpPageError) {
        return <ErrorMessage hasError title="Unable to fetch help page" />;
    }

    if (isFetchingHelpPage) {
        return <Spinner centered />;
    }

    return (
        <div className={styles.helpContainer}>
            <Tabs activeTabKey={selectedTab} onChange={setSelectedTab}>
                <Tab tabKey="resource-management" title="Resource management">
                    <div className={styles.contentContainer}>
                        <div className={styles.content}>
                            <MarkdownViewer markdown={helpPageMarkdown} />
                        </div>
                    </div>
                </Tab>
                <Tab tabKey="role-delegation" title="Role delegation">
                    <div className={styles.contentContainer}>
                        <div className={styles.content}>
                            <MarkdownViewer markdown={helpPageMarkdown} />
                        </div>
                    </div>
                </Tab>
            </Tabs>
        </div>
    );
};
export default HelpPage;

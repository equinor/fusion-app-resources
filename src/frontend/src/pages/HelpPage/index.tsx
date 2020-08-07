import * as React from 'react';
import * as styles from './styles.less';
import { Spinner, MarkdownViewer, ErrorMessage, Tabs, Tab } from '@equinor/fusion-components';

import GithubApiClient from '../../api/GithubApiClient';

const HelpPage: React.FC = () => {
    const [isFetchingHelpPage, setIsFetchingHelpPage] = React.useState<boolean>(false);
    const [helpPageError, setHelpPageError] = React.useState<Error | null>(null);
    const [cmMarkdown, setCmMarkdown] = React.useState<string>('');
    const [rdMarkdown, setRdMarkdown] = React.useState<string>('');
    const [selectedTab, setSelectedTab] = React.useState<string>('contract-management');
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
            const contractManagement = await githubApiClient.getContractManagementAsync();
            const roleDelegation = await githubApiClient.getRoleDelegationAsync();
            setCmMarkdown(contractManagement);
            setRdMarkdown(roleDelegation);
        } catch (e) {
            setHelpPageError(e);
        } finally {
            setIsFetchingHelpPage(false);
        }
    }, [githubApiClient]);

    React.useEffect(() => {
        if (githubApiClient) {
            fetchHelpPageAsync();
        }
    }, [githubApiClient]);

    if (helpPageError) {
        return <ErrorMessage hasError title="Unable to fetch help page" />;
    }

    if (isFetchingHelpPage) {
        return <Spinner centered />;
    }

    return (
        <div className={styles.helpContainer}>
            <Tabs activeTabKey={selectedTab} onChange={setSelectedTab}>
                <Tab tabKey="contract-management" title="Contract management">
                    <div className={styles.contentContainer}>
                        <div className={styles.content}>
                            <MarkdownViewer markdown={cmMarkdown} />
                        </div>
                    </div>
                </Tab>
                <Tab tabKey="role-delegation" title="Role delegation">
                    <div className={styles.contentContainer}>
                        <div className={styles.content}>
                            <MarkdownViewer markdown={rdMarkdown} />
                        </div>
                    </div>
                </Tab>
            </Tabs>
        </div>
    );
};
export default HelpPage;

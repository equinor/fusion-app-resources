
import styles from './styles.less';
import { Spinner, MarkdownViewer, ErrorMessage, Tabs, Tab } from '@equinor/fusion-components';

import GithubApiClient from '../../api/GithubApiClient';
import { FC, useState, useMemo, useCallback, useEffect } from 'react';

const HelpPage: FC = () => {
    const [isFetchingHelpPage, setIsFetchingHelpPage] = useState<boolean>(false);
    const [helpPageError, setHelpPageError] = useState<Error | null>(null);
    const [cmMarkdown, setCmMarkdown] = useState<string>('');
    const [rdMarkdown, setRdMarkdown] = useState<string>('');
    const [selectedTab, setSelectedTab] = useState<string>('contract-management');
    const githubApiClient = useMemo(
        () =>
            new GithubApiClient(
                'https://raw.githubusercontent.com/equinor/fusion-app-resources/master'
            ),
        []
    );

    const fetchHelpPageAsync = useCallback(async () => {
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

    useEffect(() => {
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
                <Tab id="contract-management-tab" tabKey="contract-management" title="Contract management">
                    <div className={styles.contentContainer}>
                        <div className={styles.content}>
                            <MarkdownViewer markdown={cmMarkdown} />
                        </div>
                    </div>
                </Tab>
                <Tab id="role-delegation-tab" tabKey="role-delegation" title="Role delegation">
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

import * as React from 'react';
import * as styles from './styles.less';
import Roles from './Roles';
import GeneralFlow from './svg/GeneralFlow';
import { Tabs, Tab } from '@equinor/fusion-components';
import { useHistory } from '@equinor/fusion';
import RequestFlow from './svg/RequestFlow';

const HelpPage: React.FC = () => {
    const [activeKey, setActiveKey] = React.useState('general-flow');
    const history = useHistory();

    const onTabKeyChange = React.useCallback(
        (tabKey: string) => {
            history.push({
                pathname: history.location.pathname,
                search: `${tabKey}`,
            });
        },
        [history.location.pathname]
    );

    React.useEffect(() => {
        const searchKey = history.location.search.slice(1);
        setActiveKey(searchKey);
    }, [history.location.search]);
    
    return (
        <div className={styles.helpContainer}>
            <Roles />
            <div className={styles.helpContent}>
                <Tabs activeTabKey={activeKey} onChange={onTabKeyChange}>
                    <Tab
                        tabKey="general-flow"
                        isCurrent={activeKey === 'general-workflow'}
                        title="General workflow"
                    >
                        <div className={styles.tabContainer}>
                            <h2 className={styles.title}>General flow</h2>
                            <GeneralFlow />
                        </div>
                    </Tab>
                    <Tab
                        tabKey="request-flow"
                        isCurrent={activeKey === 'request-flow'}
                        title="Request flow"
                    >
                         <div className={styles.tabContainer}>
                            <h2 className={styles.title}>New and Edit - Request flow</h2>
                            <RequestFlow />
                        </div>
                    </Tab>
                </Tabs>
            </div>
        </div>
    );
};
export default HelpPage;

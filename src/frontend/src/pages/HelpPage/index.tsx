import * as React from 'react';
import * as styles from './styles.less';
import GeneralFlow from './svg/GeneralFlow';
import { Tabs, Tab } from '@equinor/fusion-components';
import { useHistory } from '@equinor/fusion';
import RequestFlow from './svg/RequestFlow';
import ContractorAccount from './svg/ContractorAccount';
import EquinorAccount from './svg/EquinorAccount';
import SystemAccount from './svg/SystemAccount';

type RoleDescriptionProps = {
    title: string;
    icon: React.ReactNode;
};

const RoleDescription: React.FC<RoleDescriptionProps> = ({ title, icon, children }) => {
    return (
        <div className={styles.roleDescription}>
            <div className={styles.header}>
                <div>{icon}</div>
                <span className={styles.title}>{title} </span>
            </div>
            <div className={styles.content}>{children}</div>
        </div>
    );
};

const HelpPage: React.FC = () => {
    const [activeKey, setActiveKey] = React.useState('responsibilities');
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
            <Tabs activeTabKey={activeKey} onChange={onTabKeyChange}>
                <Tab
                    tabKey="responsibilities"
                    isCurrent={activeKey === 'responsibilities'}
                    title="Responsibilities"
                >
                    <div className={styles.tabContainer}>
                        <div className={styles.rolesContainer}>
                            <h2>Roles</h2>
                            <RoleDescription
                                title="Equinor"
                                icon={<EquinorAccount />}
                            />
                            <RoleDescription
                                title="Contractor"
                                icon={<ContractorAccount />}
                            />
                            <RoleDescription title="System account" icon={<SystemAccount />} />
                        </div>
                        <div className={styles.helpContent}>
                            <h2 className={styles.title}>Responsibilities</h2>
                            <GeneralFlow />
                        </div>
                    </div>
                </Tab>
                <Tab
                    tabKey="request-flow"
                    isCurrent={activeKey === 'request-flow'}
                    title="Personnel request flow"
                >
                    <div className={styles.tabContainer}>
                        <div className={styles.rolesContainer}>
                            <h2>Roles</h2>
                            <RoleDescription
                                title="Contractor and CR./CR"
                                icon={<ContractorAccount />}
                            >
                                Any contractor user can create a request but only the External
                                company rep. (CR.) or External contract responsible (CR), can
                                approve, reject or delete one. If rejected it is stored in Completed
                                requests.
                            </RoleDescription>
                            <RoleDescription title="Equinor CR./CR" icon={<EquinorAccount />}>
                                When the request is approved by the CR. or CR, then an Equinor
                                employee set as responsible for the contract, either Equinor company
                                rep or Equinor contract responsible, can approve or reject the
                                request. If rejected it is stored in Completed requests.
                            </RoleDescription>
                            <RoleDescription title="System account" icon={<SystemAccount />}>
                                System account is a function that is automatically triggered to
                                provision the request. This will send it to the Pro Org-chart where
                                it will now be accessible. It will also be stored in the Completed
                                requests.
                            </RoleDescription>
                        </div>

                        <div className={styles.helpContent}>
                            <h2 className={styles.title}>New and Edit - Personnel request flow</h2>
                            <RequestFlow />
                        </div>
                    </div>
                </Tab>
            </Tabs>
        </div>
    );
};
export default HelpPage;

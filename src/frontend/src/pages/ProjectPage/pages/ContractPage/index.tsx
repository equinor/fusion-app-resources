import * as React from 'react';
import { RouteComponentProps, Route } from 'react-router-dom';
import ContractContext from '../../../../contractContex';
import {
    NavigationDrawer,
    ErrorMessage,
    IconButton,
    CloseIcon,
    SkeletonBar,
    SkeletonDisc,
} from '@equinor/fusion-components';
import ScopedSwitch from '../../../../components/ScopedSwitch';
import ContractDetailsPage from './pages/ContractDetailsPage';
import ManagePersonellPage from './pages/ManagePersonnelPage';
import useContractPageNavigationStructure from './useContractPageNavigationStructure';
import ActualMppPage from './pages/ActualMppPage';
import ActiveRequestsPage from './pages/ActiveRequestsPage';
import useContractFromId from './hooks/useContractFromId';
import * as styles from './styles.less';
import { useCurrentContext, useHistory } from '@equinor/fusion';

type ContractPageMatch = {
    contractId: string;
};

type ContractPageProps = RouteComponentProps<ContractPageMatch>;

const ContractPage: React.FC<ContractPageProps> = ({ match }) => {
    const currentContext = useCurrentContext();
    const { contract, isFetchingContract } = useContractFromId(match.params.contractId);
    const { structure, setStructure } = useContractPageNavigationStructure(match.params.contractId);

    const contractContext = React.useMemo(() => {
        return {
            contract,
            isFetchingContract,
        };
    }, [contract, isFetchingContract]);

    const history = useHistory();
    const onClose = React.useCallback(() => {
        history.push('/' + currentContext?.id || '');
    }, [history, currentContext]);

    if (!contract && !isFetchingContract) {
        return (
            <div className={styles.container}>
                <header className={styles.header}>
                    <IconButton onClick={onClose}>
                        <CloseIcon />
                    </IconButton>
                </header>
                <div className={styles.content}>
                    <ErrorMessage hasError errorType="notFound" resourceName="contract" />
                </div>
            </div>
        );
    }

    return (
        <ContractContext.Provider value={contractContext}>
            <div className={styles.container}>
                <header className={styles.header}>
                    <IconButton onClick={onClose}>
                        <CloseIcon />
                    </IconButton>
                    <h2>
                        {isFetchingContract ? (
                            <SkeletonBar />
                        ) : (
                            <>
                                {contract?.contractNumber} - {contract?.name}
                            </>
                        )}
                    </h2>
                </header>
                <div className={styles.content}>
                    <div className={styles.nav}>
                        <NavigationDrawer
                            id="resources-contract-navigation-drawer"
                            structure={structure}
                            onChangeStructure={setStructure}
                        />
                    </div>

                    <div className={styles.details}>
                        <ScopedSwitch>
                            <Route path="/" exact component={ContractDetailsPage} />
                            <Route path="/personnel" component={ManagePersonellPage} />
                            <Route path="/actual-mpp" component={ActualMppPage} />
                            <Route path="/active-requests" component={ActiveRequestsPage} />
                        </ScopedSwitch>
                    </div>
                </div>
            </div>
        </ContractContext.Provider>
    );
};

export default ContractPage;

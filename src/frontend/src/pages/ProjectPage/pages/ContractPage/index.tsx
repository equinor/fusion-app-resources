import { RouteComponentProps, Route } from 'react-router-dom';
import ContractContext from '../../../../contractContex';
import {
    NavigationDrawer,
    IconButton,
    CloseIcon,
    SkeletonBar,
} from '@equinor/fusion-components';
import ScopedSwitch from '../../../../components/ScopedSwitch';
import ContractDetailsPage from './pages/ContractDetailsPage';
import ManagePersonellPage from './pages/ManagePersonnelPage';
import useContractPageNavigationStructure from './useContractPageNavigationStructure';
import ActualMppPage from './pages/ActualMppPage';
import ActiveRequestsPage from './pages/ActiveRequestsPage';
import CompletedRequestsPage from './pages/CompletedRequestsPage';
import useContractFromId from './hooks/useContractFromId';
import styles from './styles.less';
import { useCurrentContext, useHistory } from '@equinor/fusion';
import { contractReducer, createInitialState } from '../../../../reducers/contractReducer';
import useCollectionReducer from '../../../../hooks/useCollectionReducer';
import ProvisioningRequestsPage from './pages/ProvisioningRequestsPage';
import ResourceErrorMessage from '../../../../components/ResourceErrorMessage';
import { FC, useMemo, useCallback } from 'react';
import ManagePersonnelMailsPage from './pages/ManagePersonnelMailsPage';

type ContractPageMatch = {
    contractId: string;
};

type ContractPageProps = RouteComponentProps<ContractPageMatch>;

const ContractPage: FC<ContractPageProps> = ({ match }) => {
    const currentContext = useCurrentContext();
    const { contract, isFetchingContract, contractError } = useContractFromId(
        match.params.contractId
    );

    const [contractState, dispatchContractAction] = useCollectionReducer(
        match.params.contractId,
        contractReducer,
        createInitialState()
    );

    const contractContext = useMemo(() => {
        return {
            contract,
            isFetchingContract,
            contractState,
            dispatchContractAction,
        };
    }, [contract, isFetchingContract, contractState, dispatchContractAction]);

    const { structure, setStructure } = useContractPageNavigationStructure(
        match.params.contractId,
        contractContext
    );

    const history = useHistory();
    const onClose = useCallback(() => {
        history.push('/' + currentContext?.id || '');
    }, [history, currentContext]);

    if (contractError) {
        return (
            <div className={styles.container}>
                <header className={styles.header}>
                    <div id="close-contract-btn">
                    <IconButton onClick={onClose}>
                        <CloseIcon />
                    </IconButton>
                    </div>
                </header>
                <div className={styles.content}>
                    <ResourceErrorMessage error={contractError} />
                </div>
            </div>
        );
    }

    return (
        <ContractContext.Provider value={contractContext}>
            <div className={styles.container}>
                <header className={styles.header}>
                    <IconButton id="close-contract-btn" onClick={onClose}>
                        <CloseIcon />
                    </IconButton>
                    <h2>
                        {isFetchingContract && !contract ? (
                            <SkeletonBar />
                        ) : (
                            <>
                                {contract?.contractNumber} - {contract?.name}
                            </>
                        )}
                    </h2>
                </header>
                <div className={styles.content}>
                    <div data-cy="resources-contract-navigation-drawer" className={styles.nav}>
                        <NavigationDrawer
                            id="resources-contract-navigation-drawer"
                            structure={structure}
                            onChangeStructure={setStructure}
                        />
                    </div>

                    <div className={styles.details}>
                        <ScopedSwitch>
                            <Route path="/" exact component={ContractDetailsPage} />
                            <Route
                                path="/manage-personnel-mails"
                                exact
                                component={ManagePersonnelMailsPage}
                            />
                            <Route path="/manage-personnel" component={ManagePersonellPage} />

                            <Route path="/actual-mpp" component={ActualMppPage} />
                            <Route path="/active-requests" component={ActiveRequestsPage} />
                            <Route
                                path="/provisioning-requests"
                                component={ProvisioningRequestsPage}
                            />
                            <Route path="/completed-requests" component={CompletedRequestsPage} />
                        </ScopedSwitch>
                    </div>
                </div>
            </div>
        </ContractContext.Provider>
    );
};

export default ContractPage;

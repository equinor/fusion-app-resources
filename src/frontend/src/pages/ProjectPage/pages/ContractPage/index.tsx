import * as React from 'react';
import { RouteComponentProps, Route } from 'react-router-dom';
import ContractContext from '../../../../contractContex';
import { Contract, useHistory, combineUrls } from '@equinor/fusion';
import { NavigationDrawer, NavigationStructure } from '@equinor/fusion-components';
import ScopedSwitch from '../../../../components/ScopedSwitch';
import ContractDetailsPage from './pages/ContractDetailsPage';
import ManagePersonellPage from './pages/ManagePersonnelPage';
import { History } from "history";

type ContractPageMatch = {
    contractId: string;
};

type ContractPageProps = RouteComponentProps<ContractPageMatch>;

const useContractFromId = (id: string) => {
    const [contract, setContract] = React.useState<Contract | null>(null);
    const [isFetchingContract, setIsFetchingContract] = React.useState(false);
    const [fetchContractError, setFetchContractError] = React.useState<Error | null>(null);

    React.useEffect(() => {
        // TODO: fetch and set contract
        setContract({ id } as Contract);
    }, [id]);

    return { contract };
};

const createContractPath = (history: History, contractId: string, path: string) => {
    const base = history.location.pathname.split("/" + contractId)[0];
    return combineUrls(base, contractId, path);
};

const createNavItem = (history: History, contractId: string, title: string, path: string): NavigationStructure => ({
    id: title,
    title,
    type: 'section',
    isActive: history.location.pathname === createContractPath(history, contractId, path),
    onClick: () => history.push(createContractPath(history, contractId, path)),
});

const getNavigationStructure = (history: History, contractId: string): NavigationStructure[] => {
    return [
        createNavItem(history, contractId, "General", ""),
        createNavItem(history, contractId, "Manage personnel", "personnel"),
        {
            id: 'manage-mpp',
            title: 'Manage MPP',
            type: 'grouping',
            isOpen: true,
            navigationChildren: [
                createNavItem(history, contractId, "Actual MPP", "actual-mpp"),
                createNavItem(history, contractId, "Active requests", "active-requests"),
                createNavItem(history, contractId, "Log", "Log"),
            ]
        },
    ];
}

const useNavigationStructure = (contractId: string) => {
    const history = useHistory();
    const [structure, setStructure] = React.useState<NavigationStructure[]>(getNavigationStructure(history, contractId));

    React.useEffect(() => {
        setStructure(getNavigationStructure(history, contractId));
    }, [contractId, history.location.pathname]);

    return { structure, setStructure };
};

const ContractPage: React.FC<ContractPageProps> = ({ match }) => {
    const { contract } = useContractFromId(match.params.contractId);
    const { structure, setStructure } = useNavigationStructure(match.params.contractId);

    const contractContext = React.useMemo(() => {
        return contract
            ? {
                  contract,
              }
            : null;
    }, [contract]);

    return (
        <ContractContext.Provider value={contractContext}>
            <div style={{ display: 'flex', minHeight: '100%', maxHeight: '100%' }}>
                <div>
                    <NavigationDrawer
                        id="resources-contract-navigation-drawer"
                        structure={structure}
                        onChangeStructure={setStructure}
                    />
                </div>
                <div>
                    <ScopedSwitch>
                        <Route path="/" exact component={ContractDetailsPage} />
                        <Route path="/personnel" component={ManagePersonellPage} />
                    </ScopedSwitch>
                </div>
            </div>
        </ContractContext.Provider>
    );
};

export default ContractPage;

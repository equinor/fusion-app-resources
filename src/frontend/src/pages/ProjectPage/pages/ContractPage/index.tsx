import * as React from 'react';
import { RouteComponentProps, Route } from 'react-router-dom';
import ContractContext from '../../../../contractContex';
import { Contract } from '@equinor/fusion';
import { NavigationDrawer } from '@equinor/fusion-components';
import ScopedSwitch from '../../../../components/ScopedSwitch';
import ContractDetailsPage from './pages/ContractDetailsPage';
import ManagePersonellPage from './pages/ManagePersonnelPage';
import useContractPageNavigationStructure from "./useContractPageNavigationStructure";
import ActualMppPage from './pages/ActualMppPage';

type ContractPageMatch = {
    contractId: string;
};

type ContractPageProps = RouteComponentProps<ContractPageMatch>;

const useContractFromId = (id: string) => {
    const [contract, setContract] = React.useState<Contract | null>(null);
    const [] = React.useState(false);
    const [] = React.useState<Error | null>(null);

    React.useEffect(() => {
        // TODO: fetch and set contract
        setContract({ id } as Contract);
    }, [id]);

    return { contract };
};


const ContractPage: React.FC<ContractPageProps> = ({ match }) => {
    const { contract } = useContractFromId(match.params.contractId);
    const { structure, setStructure } = useContractPageNavigationStructure(match.params.contractId);

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
                <ScopedSwitch>
                    <Route path="/" exact component={ContractDetailsPage} />
                    <Route path="/personnel" component={ManagePersonellPage} />
                    <Route path="/actual-mpp" component={ActualMppPage} />
                </ScopedSwitch>
            </div>
        </ContractContext.Provider>
    );
};

export default ContractPage;

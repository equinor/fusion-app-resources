import * as React from 'react';
import { Route } from 'react-router-dom';
import ContractsOverviewPage from './pages/ContractsOverviewPage';
import AllocateContractPage from './pages/AllocateContractPage';
import ContractPage from './pages/ContractPage';
import ScopedSwitch from '../../components/ScopedSwitch';
import EditContractPage from './pages/ContractPage/pages/EditContractPage';

const ProjectPage = () => {
    return (
        <ScopedSwitch>
            <Route path="/" exact component={ContractsOverviewPage} />
            <Route path="/allocate" exact component={AllocateContractPage} />
            <Route path="/:contractId/edit" exact component={EditContractPage} />
            <Route path="/:contractId" component={ContractPage} />
        </ScopedSwitch>
    );
};

export default ProjectPage;

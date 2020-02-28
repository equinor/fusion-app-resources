import * as React from 'react';
import { Route } from 'react-router-dom';
import ContractsOverviewPage from './pages/ContractsOverviewPage';
import ContractPage from './pages/ContractPage';
import ScopedSwitch from '../../components/ScopedSwitch';
import ManagePersonellPage from './pages/ContractPage/pages/ManagePersonnelPage';

const ProjectPage = () => {

    return (
        <ScopedSwitch>
            <Route path="/" exact component={ContractsOverviewPage} />
            <Route path="/:contractId" component={ContractPage} />
            <Route path="/:contractId/managedpersonnel" component={ManagePersonellPage} />
        </ScopedSwitch>
    );
}

export default ProjectPage;
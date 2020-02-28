import * as React from 'react';
import { Route } from 'react-router-dom';
import ContractsOverview from './pages/ContractsOverview';
import ContractPage from './pages/ContractPage';
import ScopedSwitch from '../../components/ScopedSwitch';

const ProjectPage = () => {
    return (
        <ScopedSwitch>
            <Route path="/" exact component={ContractsOverview} />
            <Route path="/:contractId" component={ContractPage} />
        </ScopedSwitch>
    );
}

export default ProjectPage;
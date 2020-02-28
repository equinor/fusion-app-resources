import * as React from 'react';
import { Route } from 'react-router-dom';
import ContractsTablePage from "./pages/ContractsTablePage";
import AllocateContractPage from "./pages/AllocateContractPage";
import ScopedSwitch from '../../../../components/ScopedSwitch';

const ProjectPage = () => {
    return (
        <ScopedSwitch>
            <Route path="/allocate" exact component={AllocateContractPage} />
            <Route path="/" component={ContractsTablePage} />
        </ScopedSwitch>
    );
}

export default ProjectPage;
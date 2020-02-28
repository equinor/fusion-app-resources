import * as React from 'react';
import { useCurrentContext } from '@equinor/fusion';

const ContractsOverviewPage = () => {
    const currentProject = useCurrentContext();

    if (!currentProject) {
        return null;
    }

    return (
        <div>
            <h1>{currentProject.title}</h1>
        </div>
    );
};

export default ContractsOverviewPage;

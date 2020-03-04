import * as React from 'react';
import { useContractContext } from '../../../../../contractContex';

const ManagePersonellPage = () => {
    const contractContext = useContractContext();

    if (!contractContext.contract) {
        return null;
    }

    return (
        <div>
            <h1>{contractContext.contract.id}</h1>
            <h2>Manage personell</h2>
        </div>
    );
};

export default ManagePersonellPage;
